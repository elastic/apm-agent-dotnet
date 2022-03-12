// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::{
    cil::uncompress_token,
    error::Error,
    ffi::{
        mdAssembly, mdAssemblyRef, mdMemberRef, mdMemberRefNil, mdModule, mdToken, mdTokenNil,
        mdTypeRef, mdTypeSpec, AppDomainID, AssemblyID, CorAssemblyFlags, CorCallingConvention,
        CorElementType, CorTokenType, ModuleID, ASSEMBLYMETADATA, BYTE, CLDB_E_RECORD_NOTFOUND,
        COR_PRF_MODULE_FLAGS, COR_SIGNATURE, E_FAIL, ULONG, WCHAR,
    },
    interfaces::{
        IMetaDataAssemblyEmit, IMetaDataAssemblyImport, IMetaDataEmit2, IMetaDataImport2,
    },
    profiler::sig::parse_number,
};
use com::sys::{GUID, HRESULT};
use core::fmt;
use crypto::{digest::Digest, sha1::Sha1};
use num_traits::FromPrimitive;
use serde::{
    de,
    de::{DeserializeOwned, Visitor},
    Deserialize, Deserializer,
};
use std::{
    cmp::Ordering,
    collections::{BTreeMap, HashMap, HashSet},
    fmt::{Display, Formatter},
    iter::repeat,
    marker::PhantomData,
    str::FromStr,
};
use widestring::U16CString;

pub(crate) struct ModuleInfo {
    pub id: ModuleID,
    pub path: String,
    pub assembly: AssemblyInfo,
    pub flags: COR_PRF_MODULE_FLAGS,
}
impl ModuleInfo {
    pub fn is_windows_runtime(&self) -> bool {
        self.flags
            .contains(COR_PRF_MODULE_FLAGS::COR_PRF_MODULE_WINDOWS_RUNTIME)
    }
}

pub(crate) struct AssemblyInfo {
    pub id: AssemblyID,
    pub name: String,
    pub manifest_module_id: ModuleID,
    pub app_domain_id: AppDomainID,
    pub app_domain_name: String,
}

#[derive(Debug, Eq, PartialEq, Clone)]
pub struct MethodSignature {
    data: Vec<u8>,
}

impl From<&[u8]> for MethodSignature {
    fn from(data: &[u8]) -> Self {
        MethodSignature::new(data.to_vec())
    }
}

impl MethodSignature {
    pub fn new(data: Vec<BYTE>) -> Self {
        Self { data }
    }

    pub fn len(&self) -> usize {
        self.data.len()
    }

    pub fn bytes(&self) -> &[u8] {
        self.data.as_slice()
    }

    pub fn calling_convention(&self) -> CorCallingConvention {
        if self.data.is_empty() {
            CorCallingConvention::IMAGE_CEE_CS_CALLCONV_DEFAULT
        } else {
            // assume the unwrap is safe here
            CorCallingConvention::from_bits(self.data[0]).unwrap()
        }
    }

    pub fn type_arguments_len(&self) -> u8 {
        if self.data.len() > 1 && self.calling_convention().is_generic() {
            self.data[1]
        } else {
            0
        }
    }

    pub fn arguments_len(&self) -> u8 {
        if self.data.len() > 2 && self.calling_convention().is_generic() {
            self.data[2]
        } else if self.data.len() > 1 {
            self.data[1]
        } else {
            0
        }
    }

    pub fn return_type_is_object(&self) -> bool {
        if self.data.len() > 2 && self.calling_convention().is_generic() {
            CorElementType::from_u8(self.data[3]) == Some(CorElementType::ELEMENT_TYPE_OBJECT)
        } else if self.data.len() > 1 {
            CorElementType::from_u8(self.data[2]) == Some(CorElementType::ELEMENT_TYPE_OBJECT)
        } else {
            false
        }
    }

    pub fn index_of_return_type(&self) -> usize {
        if self.data.len() > 2 && self.calling_convention().is_generic() {
            3
        } else if self.data.len() > 1 {
            2
        } else {
            0
        }
    }

    pub fn is_instance_method(&self) -> bool {
        self.calling_convention()
            .contains(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_HASTHIS)
    }
}

struct MethodSignatureVisitor;
impl<'de> Visitor<'de> for MethodSignatureVisitor {
    type Value = MethodSignature;

    fn expecting(&self, formatter: &mut fmt::Formatter) -> fmt::Result {
        formatter.write_str("a string")
    }

    fn visit_str<E>(self, v: &str) -> Result<Self::Value, E>
    where
        E: de::Error,
    {
        let parse_bytes: Result<Vec<_>, _> = v.split(' ').map(hex::decode).collect();
        match parse_bytes {
            Ok(b) => Ok(MethodSignature::new(b.into_iter().flatten().collect())),
            Err(e) => Err(de::Error::custom(format!(
                "Could not parse MethodSignature: {:?}",
                e.to_string()
            ))),
        }
    }
}

impl<'de> Deserialize<'de> for MethodSignature {
    fn deserialize<D>(deserializer: D) -> Result<MethodSignature, D::Error>
    where
        D: Deserializer<'de>,
    {
        deserializer.deserialize_str(MethodSignatureVisitor)
    }
}

#[derive(Debug, Eq, PartialEq, Clone)]
pub struct AssemblyReference {
    pub name: String,
    pub version: Version,
    pub locale: String,
    pub public_key: PublicKeyToken,
}

impl AssemblyReference {
    pub fn new<S: Into<String>>(
        name: S,
        version: Version,
        locale: S,
        public_key: PublicKeyToken,
    ) -> Self {
        Self {
            name: name.into(),
            version,
            locale: locale.into(),
            public_key,
        }
    }
}

impl Display for AssemblyReference {
    fn fmt(&self, f: &mut Formatter<'_>) -> fmt::Result {
        write!(
            f,
            "{}, Version={}, Culture={}, PublicKeyToken={}",
            &self.name, &self.version, &self.locale, &self.public_key.0
        )
    }
}

impl FromStr for AssemblyReference {
    type Err = Error;

    fn from_str(s: &str) -> Result<Self, Self::Err> {
        let mut parts: Vec<&str> = s.split(',').map(|p| p.trim()).collect();

        if parts.len() != 4 {
            return Err(Error::InvalidAssemblyReference);
        }

        let name = parts.remove(0).to_string();
        let map: BTreeMap<&str, &str> = parts
            .iter()
            .map(|p| {
                let pp: Vec<&str> = p.split('=').map(|pp| pp.trim()).collect();
                (pp[0], pp[1])
            })
            .collect();

        let version = Version::from_str(map["Version"])?;
        let locale = map["Culture"].to_string();
        let public_key = PublicKeyToken(map["PublicKeyToken"].to_string());
        Ok(AssemblyReference {
            name,
            version,
            locale,
            public_key,
        })
    }
}

impl<'de> Deserialize<'de> for AssemblyReference {
    fn deserialize<D>(deserializer: D) -> Result<AssemblyReference, D::Error>
    where
        D: Deserializer<'de>,
    {
        deserialize_from_str(deserializer)
    }
}

/// deserializes any type that implements FromStr from a str
pub(crate) fn deserialize_from_str<'de, T, D>(deserializer: D) -> Result<T, D::Error>
where
    T: Deserialize<'de> + FromStr<Err = Error>,
    D: Deserializer<'de>,
{
    // This is a Visitor that forwards string types to T's `FromStr` impl and
    // forwards map types to T's `Deserialize` impl. The `PhantomData` is to
    // keep the compiler from complaining about T being an unused generic type
    // parameter. We need T in order to know the Value type for the Visitor
    // impl.
    struct String<T>(PhantomData<fn() -> T>);

    impl<'de, T> Visitor<'de> for String<T>
    where
        T: Deserialize<'de> + FromStr<Err = Error>,
    {
        type Value = T;

        fn expecting(&self, formatter: &mut fmt::Formatter) -> fmt::Result {
            formatter.write_str("a string")
        }

        fn visit_str<E>(self, value: &str) -> Result<T, E>
        where
            E: de::Error,
        {
            FromStr::from_str(value).map_err(|e| E::custom(format!("{:?}", e)))
        }
    }

    deserializer.deserialize_str(String(PhantomData))
}

#[derive(Debug, Eq, PartialEq, Clone)]
pub struct PublicKeyToken(String);

impl PublicKeyToken {
    pub fn new<S: Into<String>>(str: S) -> Self {
        Self(str.into())
    }

    pub fn into_bytes(&self) -> Vec<BYTE> {
        hex::decode(&self.0).unwrap()
    }
}

#[derive(Debug, Eq, PartialEq, Deserialize, Clone)]
pub struct CallerMethodReference {
    pub(crate) assembly: String,
    #[serde(rename = "type")]
    pub(crate) type_name: String,
    #[serde(rename = "method")]
    pub(crate) method_name: String,
}

#[derive(Debug, Eq, PartialEq, Deserialize, Clone)]
pub struct WrapperMethodReference {
    pub(crate) assembly: AssemblyReference,
    #[serde(rename = "type")]
    pub(crate) type_name: String,
    #[serde(rename = "method")]
    pub(crate) method_name: Option<String>,
    pub(crate) action: String,
    #[serde(rename = "signature")]
    pub(crate) method_signature: Option<MethodSignature>,
}

impl WrapperMethodReference {
    pub fn get_type_cache_key(&self) -> String {
        format!("[{}]{}", &self.assembly.name, &self.type_name,)
    }

    pub fn get_method_cache_key(&self) -> String {
        format!(
            "[{}]{}.{}",
            &self.assembly.name,
            &self.type_name,
            self.method_name.as_ref().map_or("", |m| m.as_str()),
        )
    }

    pub fn full_name(&self) -> String {
        format!(
            "{}.{}",
            &self.type_name,
            self.method_name.as_ref().map_or("", |m| m.as_str())
        )
    }
}

#[derive(Debug, Eq, PartialEq, Deserialize, Clone)]
pub struct TargetMethodReference {
    assembly: String,
    #[serde(rename = "type")]
    type_name: String,
    #[serde(rename = "method")]
    method_name: String,
    #[serde(default = "version_max", deserialize_with = "deserialize_max_version")]
    maximum_version: Version,
    #[serde(default = "version_min")]
    minimum_version: Version,
    signature_types: Option<Vec<String>>,
}

/// deserializes a [Version], defaulting any missing values to [u16::MAX]
fn deserialize_max_version<'de, D>(deserializer: D) -> Result<Version, D::Error>
where
    D: Deserializer<'de>,
{
    struct VersionVisitor;
    impl<'de> Visitor<'de> for VersionVisitor {
        type Value = Version;
        fn expecting(&self, formatter: &mut fmt::Formatter) -> fmt::Result {
            formatter.write_str("a string")
        }

        fn visit_str<E>(self, value: &str) -> Result<Self::Value, E>
        where
            E: de::Error,
        {
            Self::Value::parse(value, u16::MAX).map_err(|e| E::custom(format!("{:?}", e)))
        }
    }

    deserializer.deserialize_str(VersionVisitor)
}

fn version_max() -> Version {
    Version::MAX
}
fn version_min() -> Version {
    Version::MIN
}

impl TargetMethodReference {
    pub fn assembly(&self) -> &str {
        &self.assembly
    }

    pub fn method_name(&self) -> &str {
        &self.method_name
    }

    pub fn type_name(&self) -> &str {
        &self.type_name
    }

    pub fn signature_types(&self) -> Option<&[String]> {
        self.signature_types.as_ref().map(|s| s.as_slice())
    }

    pub fn is_valid_for_assembly(&self, assembly_name: &str, version: &Version) -> bool {
        if &self.assembly != assembly_name {
            return false;
        }

        if &self.minimum_version > version {
            return false;
        }

        if &self.maximum_version < version {
            return false;
        }

        true
    }
}

/// The method replacement
#[derive(Debug, Eq, PartialEq, Deserialize, Clone)]
pub struct MethodReplacement {
    /// The caller
    #[serde(default)]
    #[serde(deserialize_with = "empty_struct_is_none")]
    caller: Option<CallerMethodReference>,
    /// The target for instrumentation
    target: Option<TargetMethodReference>,
    /// The wrapper providing the instrumentation
    wrapper: Option<WrapperMethodReference>,
}

impl MethodReplacement {
    pub fn caller(&self) -> Option<&CallerMethodReference> {
        self.caller.as_ref()
    }

    pub fn target(&self) -> Option<&TargetMethodReference> {
        self.target.as_ref()
    }

    pub fn wrapper(&self) -> Option<&WrapperMethodReference> {
        self.wrapper.as_ref()
    }
}

/// Deserializes a T to Option::Some(T) and an empty struct to Option::None
fn empty_struct_is_none<'de, T, D>(deserializer: D) -> Result<Option<T>, D::Error>
where
    T: DeserializeOwned,
    D: Deserializer<'de>,
{
    #[derive(Deserialize)]
    #[serde(untagged)]
    enum EmptyOption<T> {
        Data(T),
        Empty {},
    }

    match EmptyOption::deserialize(deserializer)? {
        EmptyOption::Data(data) => Ok(Some(data)),
        EmptyOption::Empty {} => Ok(None),
    }
}

#[derive(Debug, Eq, PartialEq, Deserialize, Clone)]
pub struct IntegrationMethod {
    pub(crate) name: String,
    pub(crate) method_replacement: MethodReplacement,
}

#[derive(Debug, Eq, PartialEq, Deserialize)]
pub struct Integration {
    pub(crate) name: String,
    pub(crate) method_replacements: Vec<MethodReplacement>,
}

#[derive(Debug, Clone)]
pub struct ModuleWrapperTokens {
    failed_wrapper_keys: HashSet<String>,
    wrapper_refs: HashMap<String, mdMemberRef>,
    wrapper_parent_type: HashMap<String, mdTypeRef>,
}

impl ModuleWrapperTokens {
    pub fn new() -> Self {
        Self {
            failed_wrapper_keys: HashSet::new(),
            wrapper_refs: HashMap::new(),
            wrapper_parent_type: HashMap::new(),
        }
    }

    pub fn is_failed_wrapper_member_key(&self, key: &str) -> bool {
        self.failed_wrapper_keys.contains(key)
    }

    pub fn contains_wrapper_member_ref(&self, key: &str) -> bool {
        self.wrapper_refs.contains_key(key)
    }

    pub fn get_wrapper_member_ref(&self, key: &str) -> Option<mdMemberRef> {
        self.wrapper_refs.get(key).copied()
    }

    pub fn get_wrapper_parent_type_ref(&self, key: &str) -> Option<mdTypeRef> {
        self.wrapper_parent_type.get(key).copied()
    }

    pub fn set_wrapper_parent_type_ref<S: Into<String>>(&mut self, key: S, type_ref: mdTypeRef) {
        self.wrapper_parent_type.insert(key.into(), type_ref);
    }

    pub fn set_failed_wrapper_member_key<S: Into<String>>(&mut self, key: S) {
        self.failed_wrapper_keys.insert(key.into());
    }

    pub fn set_wrapper_member_ref<S: Into<String>>(&mut self, key: S, member_ref: mdMemberRef) {
        self.wrapper_refs.insert(key.into(), member_ref);
    }
}

#[derive(Debug, Clone)]
pub struct ModuleMetadata {
    pub import: IMetaDataImport2,
    pub emit: IMetaDataEmit2,
    pub assembly_import: IMetaDataAssemblyImport,
    pub assembly_emit: IMetaDataAssemblyEmit,
    pub assembly_name: String,
    pub app_domain_id: AppDomainID,
    pub module_version_id: GUID,
    pub integrations: Vec<IntegrationMethod>,
    pub(crate) cor_assembly_property: AssemblyMetaData,
}

impl ModuleMetadata {
    pub fn new(
        import: IMetaDataImport2,
        emit: IMetaDataEmit2,
        assembly_import: IMetaDataAssemblyImport,
        assembly_emit: IMetaDataAssemblyEmit,
        assembly_name: String,
        app_domain_id: AppDomainID,
        module_version_id: GUID,
        integrations: Vec<IntegrationMethod>,
        cor_assembly_property: AssemblyMetaData,
    ) -> Self {
        Self {
            import,
            emit,
            assembly_import,
            assembly_emit,
            assembly_name,
            app_domain_id,
            module_version_id,
            integrations,
            cor_assembly_property,
        }
    }

    pub fn get_method_replacements_for_caller(
        &self,
        caller: &FunctionInfo,
    ) -> Vec<MethodReplacement> {
        self.integrations
            .iter()
            .filter_map(|i| {
                if let Some(caller_ref) = &i.method_replacement.caller {
                    if caller_ref.type_name.is_empty()
                        || caller
                            .type_info
                            .as_ref()
                            .map_or(false, |t| t.name == caller_ref.type_name)
                            && caller_ref.method_name.is_empty()
                        || caller.name == caller_ref.method_name
                    {
                        Some(&i.method_replacement)
                    } else {
                        None
                    }
                } else {
                    Some(&i.method_replacement)
                }
            })
            .cloned()
            .collect()
    }
}

pub struct MetadataBuilder<'a> {
    module_metadata: &'a ModuleMetadata,
    module_wrapper_tokens: &'a mut ModuleWrapperTokens,
    module: mdModule,
}

impl<'a> MetadataBuilder<'a> {
    pub fn new(
        module_metadata: &'a ModuleMetadata,
        module_wrapper_tokens: &'a mut ModuleWrapperTokens,
        module: mdModule,
    ) -> Self {
        Self {
            module_metadata,
            module_wrapper_tokens,
            module,
        }
    }

    pub fn emit_assembly_ref(&self, assembly_reference: &AssemblyReference) -> Result<(), HRESULT> {
        let (sz_locale, cb_locale) = if &assembly_reference.locale == "neutral" {
            (std::ptr::null_mut() as *mut WCHAR, 0)
        } else {
            let wstr = U16CString::from_str(&assembly_reference.locale).unwrap();
            let len = wstr.len() as ULONG;
            (wstr.into_vec().as_mut_ptr(), len)
        };

        let assembly_metadata = ASSEMBLYMETADATA {
            usMajorVersion: assembly_reference.version.major,
            usMinorVersion: assembly_reference.version.minor,
            usBuildNumber: assembly_reference.version.build,
            usRevisionNumber: assembly_reference.version.revision,
            szLocale: sz_locale,
            cbLocale: cb_locale,
            rProcessor: std::ptr::null_mut(),
            ulProcessor: 0,
            rOS: std::ptr::null_mut(),
            ulOS: 0,
        };

        let public_key_bytes = assembly_reference.public_key.into_bytes();
        let assembly_ref = self
            .module_metadata
            .assembly_emit
            .define_assembly_ref(
                &public_key_bytes,
                &assembly_reference.name,
                &assembly_metadata,
                &[],
                CorAssemblyFlags::empty(),
            )
            .map_err(|e| {
                log::warn!(
                    "DefineAssemblyRef failed for assembly {} on module={} version_id={:?}",
                    &assembly_reference.name,
                    &self.module_metadata.assembly_name,
                    &self.module_metadata.module_version_id
                );
                e
            })?;

        Ok(())
    }

    pub fn find_wrapper_type_ref(
        &mut self,
        wrapper: &WrapperMethodReference,
    ) -> Result<mdTypeRef, HRESULT> {
        let cache_key = wrapper.get_type_cache_key();
        if let Some(type_ref) = self
            .module_wrapper_tokens
            .get_wrapper_parent_type_ref(&cache_key)
        {
            return Ok(type_ref);
        }

        // check if the type is defined in this module's assembly
        let type_ref = if self.module_metadata.assembly_name == wrapper.assembly.name {
            self.module_metadata
                .emit
                .define_type_ref_by_name(self.module, &wrapper.type_name)
        } else {
            match self
                .module_metadata
                .assembly_import
                .find_assembly_ref(&wrapper.assembly.name)
            {
                Some(assembly_ref) => {
                    match self
                        .module_metadata
                        .import
                        .find_type_ref(assembly_ref, &wrapper.type_name)
                    {
                        Ok(t) => Ok(t),
                        Err(e) => {
                            if e == CLDB_E_RECORD_NOTFOUND {
                                self.module_metadata
                                    .emit
                                    .define_type_ref_by_name(assembly_ref, &wrapper.type_name)
                            } else {
                                log::warn!("error defining type ref for {}", &wrapper.type_name);
                                Err(e)
                            }
                        }
                    }
                }
                None => {
                    log::warn!(
                        "Assembly reference not found for {}",
                        &wrapper.assembly.name
                    );
                    return Err(E_FAIL);
                }
            }
        }?;

        self.module_wrapper_tokens
            .set_wrapper_parent_type_ref(cache_key, type_ref);
        Ok(type_ref)
    }

    pub fn store_wrapper_method_ref(
        &mut self,
        wrapper: &WrapperMethodReference,
    ) -> Result<(), HRESULT> {
        let cache_key = wrapper.get_method_cache_key();
        if self
            .module_wrapper_tokens
            .contains_wrapper_member_ref(&cache_key)
        {
            return Ok(());
        }

        let type_ref = self.find_wrapper_type_ref(wrapper).map_err(|e| {
            log::warn!("failed finding wrapper method ref {}", e);
            self.module_wrapper_tokens
                .set_failed_wrapper_member_key(&cache_key);
            e
        })?;

        let mut member_ref = mdMemberRefNil;
        if let Some(signature) = &wrapper.method_signature {
            if signature.len() > 0 {
                if let Some(method_name) = &wrapper.method_name {
                    match self.module_metadata.import.find_member_ref(
                        type_ref,
                        method_name,
                        &signature.data,
                    ) {
                        Ok(m) => member_ref = m,
                        Err(e) => {
                            if e == CLDB_E_RECORD_NOTFOUND {
                                match self.module_metadata.emit.define_member_ref(
                                    type_ref,
                                    method_name,
                                    &signature.data,
                                ) {
                                    Ok(m) => member_ref = m,
                                    Err(e) => {
                                        self.module_wrapper_tokens
                                            .set_failed_wrapper_member_key(&cache_key);
                                        return Err(e);
                                    }
                                }
                            } else {
                                self.module_wrapper_tokens
                                    .set_failed_wrapper_member_key(&cache_key);
                                return Err(e);
                            }
                        }
                    }
                }
            }
        }

        self.module_wrapper_tokens
            .set_wrapper_member_ref(&cache_key, member_ref);
        Ok(())
    }
}

#[derive(Debug, Clone)]
pub struct FunctionInfo {
    pub id: mdToken,
    pub name: String,
    pub type_info: Option<TypeInfo>,
    pub is_generic: bool,
    pub signature: MethodSignature,
    pub function_spec_signature: Option<MethodSignature>,
    pub method_def_id: mdToken,
    pub method_signature: FunctionMethodSignature,
}

impl FunctionInfo {
    pub fn new(
        id: mdToken,
        name: String,
        is_generic: bool,
        type_info: Option<TypeInfo>,
        signature: MethodSignature,
        function_spec_signature: Option<MethodSignature>,
        method_def_id: mdToken,
        method_signature: FunctionMethodSignature,
    ) -> Self {
        Self {
            id,
            name,
            type_info,
            is_generic,
            signature,
            function_spec_signature,
            method_def_id,
            method_signature,
        }
    }

    /// Full type and function name
    pub fn full_name(&self) -> String {
        match &self.type_info {
            Some(t) => format!("{}.{}", &t.name, &self.name),
            None => format!(".{}", &self.name),
        }
    }
}

#[derive(Debug, Clone)]
pub struct FunctionMethodSignature {
    pub data: Vec<COR_SIGNATURE>,
}

impl FunctionMethodSignature {
    pub fn new(data: Vec<COR_SIGNATURE>) -> Self {
        Self { data }
    }

    fn parse_type_def_or_ref_encoded(signature: &[u8]) -> Option<(usize, ULONG, ULONG)> {
        if let Some((number, num_idx)) = parse_number(signature) {
            let index_type = number & 0x03;
            let index = number >> 2;
            Some((num_idx, index_type, index))
        } else {
            None
        }
    }

    fn parse_type(signature: &[u8]) -> Option<(usize, usize)> {
        let start_idx = 0;
        let mut idx = start_idx;
        if let Some(elem_type) = CorElementType::from_u8(signature[idx]) {
            idx += 1;
            match elem_type {
                CorElementType::ELEMENT_TYPE_VOID
                | CorElementType::ELEMENT_TYPE_BOOLEAN
                | CorElementType::ELEMENT_TYPE_CHAR
                | CorElementType::ELEMENT_TYPE_I1
                | CorElementType::ELEMENT_TYPE_U1
                | CorElementType::ELEMENT_TYPE_I2
                | CorElementType::ELEMENT_TYPE_U2
                | CorElementType::ELEMENT_TYPE_I4
                | CorElementType::ELEMENT_TYPE_U4
                | CorElementType::ELEMENT_TYPE_I8
                | CorElementType::ELEMENT_TYPE_U8
                | CorElementType::ELEMENT_TYPE_R4
                | CorElementType::ELEMENT_TYPE_R8
                | CorElementType::ELEMENT_TYPE_STRING
                | CorElementType::ELEMENT_TYPE_OBJECT => Some((start_idx, idx)),
                CorElementType::ELEMENT_TYPE_PTR => None,
                CorElementType::ELEMENT_TYPE_CLASS | CorElementType::ELEMENT_TYPE_VALUETYPE => {
                    Self::parse_type_def_or_ref_encoded(&signature[idx..])
                        .map(|(type_idx, _, _)| (start_idx, idx + type_idx))
                }
                CorElementType::ELEMENT_TYPE_SZARRAY => {
                    if signature.len() == 1 {
                        return None;
                    }

                    if signature[idx] == CorElementType::ELEMENT_TYPE_CMOD_OPT as u8
                        || signature[idx] == CorElementType::ELEMENT_TYPE_CMOD_REQD as u8
                    {
                        None
                    } else {
                        Self::parse_type(&signature[idx..]).map(|(_, e)| (start_idx, idx + e))
                    }
                }
                CorElementType::ELEMENT_TYPE_GENERICINST => {
                    if signature.len() == 1 {
                        return None;
                    }

                    if signature[idx] != CorElementType::ELEMENT_TYPE_CLASS as u8
                        && signature[idx] != CorElementType::ELEMENT_TYPE_VALUETYPE as u8
                    {
                        return None;
                    }

                    idx += 1;

                    if let Some((type_idx, _, _)) =
                        Self::parse_type_def_or_ref_encoded(&signature[idx..])
                    {
                        idx += type_idx;
                    } else {
                        return None;
                    }

                    let num;
                    if let Some((number, num_idx)) =
                        crate::profiler::sig::parse_number(&signature[idx..])
                    {
                        idx += num_idx;
                        num = number;
                    } else {
                        return None;
                    }

                    for _ in 0..num {
                        if let Some((_, end_idx)) = Self::parse_type(&signature[idx..]) {
                            idx += end_idx;
                        } else {
                            return None;
                        }
                    }

                    Some((start_idx, idx))
                }
                CorElementType::ELEMENT_TYPE_VAR | CorElementType::ELEMENT_TYPE_MVAR => {
                    parse_number(&signature[idx..]).map(|(_, num_idx)| (start_idx, idx + num_idx))
                }
                _ => None,
            }
        } else {
            None
        }
    }

    fn parse_return_type(signature: &[u8]) -> Option<(usize, usize)> {
        let start_idx = 0;
        let mut idx = start_idx;
        if let Some(elem_type) = CorElementType::from_u8(signature[idx]) {
            if elem_type == CorElementType::ELEMENT_TYPE_CMOD_OPT
                || elem_type == CorElementType::ELEMENT_TYPE_CMOD_REQD
            {
                return None;
            }

            if elem_type == CorElementType::ELEMENT_TYPE_TYPEDBYREF {
                return None;
            }

            if elem_type == CorElementType::ELEMENT_TYPE_VOID {
                idx += 1;
                return Some((start_idx, idx));
            }

            if elem_type == CorElementType::ELEMENT_TYPE_BYREF {
                idx += 1;
            }

            Self::parse_type(&signature[idx..]).map(|(_, end_idx)| (start_idx, idx + end_idx))
        } else {
            None
        }
    }

    pub(crate) fn calling_convention(&self) -> Option<CorCallingConvention> {
        if self.data.is_empty() {
            None
        } else {
            CorCallingConvention::from_bits(self.data[0])
        }
    }

    fn parse_param(signature: &[u8]) -> Option<(usize, usize)> {
        let mut idx = 0;
        if signature[idx] == CorElementType::ELEMENT_TYPE_CMOD_OPT as u8
            || signature[idx] == CorElementType::ELEMENT_TYPE_CMOD_REQD as u8
        {
            return None;
        }

        if signature[idx] == CorElementType::ELEMENT_TYPE_TYPEDBYREF as u8 {
            return None;
        }

        if signature[idx] == CorElementType::ELEMENT_TYPE_BYREF as u8 {
            idx += 1;
        }

        Self::parse_type(&signature[idx..]).map(|(s, e)| (idx, idx + e))
    }

    pub fn try_parse(&self) -> Option<ParsedFunctionMethodSignature> {
        if let Some(calling_convention) = self.calling_convention() {
            let mut idx = 1;

            // generic type parameters length
            let type_arg_len = if calling_convention.is_generic() {
                if let Some((number, num_idx)) = parse_number(&self.data[idx..]) {
                    idx += num_idx;
                    number as u8
                } else {
                    return None;
                }
            } else {
                0
            };

            // parameters length
            let arg_len = if let Some((number, num_idx)) = parse_number(&self.data[idx..]) {
                idx += num_idx;
                number as u8
            } else {
                return None;
            };

            let ret_type = match Self::parse_return_type(&self.data[idx..]) {
                Some((_, end_idx)) => {
                    let ret_type = (idx, idx + end_idx);
                    idx += end_idx;
                    ret_type
                }
                None => return None,
            };

            let mut sentinel_found = false;
            let mut params = vec![];
            for _ in 0..arg_len {
                if self.data[idx] == CorElementType::ELEMENT_TYPE_SENTINEL as u8 {
                    if sentinel_found {
                        return None;
                    }

                    sentinel_found = true;
                    idx += 1;
                }

                if let Some(param) = Self::parse_param(&self.data[idx..]) {
                    params.push((idx, idx + param.1));
                    idx += param.1;
                } else {
                    return None;
                }
            }

            Some(ParsedFunctionMethodSignature {
                data: self.data.clone(),
                type_arg_len,
                arg_len,
                ret_type,
                args: params,
            })
        } else {
            None
        }
    }
}

pub struct FunctionMethodArgument<'a> {
    data: &'a [u8],
}

impl<'a> FunctionMethodArgument<'a> {
    pub fn new(data: &'a [u8]) -> Self {
        Self { data }
    }

    pub fn signature(&self) -> &[u8] {
        self.data
    }

    pub fn get_type_flags(&self) -> (CorElementType, MethodArgumentTypeFlag) {
        let mut idx = 0;
        if self.data[idx] == CorElementType::ELEMENT_TYPE_VOID as u8 {
            return (
                CorElementType::ELEMENT_TYPE_VOID,
                MethodArgumentTypeFlag::VOID,
            );
        }

        let mut flags = MethodArgumentTypeFlag::empty();
        if self.data[idx] == CorElementType::ELEMENT_TYPE_BYREF as u8 {
            flags |= MethodArgumentTypeFlag::BY_REF;
            idx += 1;
        }

        let element_type = CorElementType::from_u8(self.data[idx]).unwrap();
        match element_type {
            CorElementType::ELEMENT_TYPE_BOOLEAN
            | CorElementType::ELEMENT_TYPE_CHAR
            | CorElementType::ELEMENT_TYPE_I1
            | CorElementType::ELEMENT_TYPE_U1
            | CorElementType::ELEMENT_TYPE_I2
            | CorElementType::ELEMENT_TYPE_U2
            | CorElementType::ELEMENT_TYPE_I4
            | CorElementType::ELEMENT_TYPE_U4
            | CorElementType::ELEMENT_TYPE_I8
            | CorElementType::ELEMENT_TYPE_U8
            | CorElementType::ELEMENT_TYPE_R4
            | CorElementType::ELEMENT_TYPE_R8
            | CorElementType::ELEMENT_TYPE_I
            | CorElementType::ELEMENT_TYPE_U
            | CorElementType::ELEMENT_TYPE_VALUETYPE
            | CorElementType::ELEMENT_TYPE_VAR
            | CorElementType::ELEMENT_TYPE_MVAR => {
                flags |= MethodArgumentTypeFlag::BOXED_TYPE;
            }
            CorElementType::ELEMENT_TYPE_GENERICINST => {
                idx += 1;
                if self.data[idx] == CorElementType::ELEMENT_TYPE_VALUETYPE as u8 {
                    flags |= MethodArgumentTypeFlag::BOXED_TYPE;
                }
            }
            _ => (),
        }

        (element_type, flags)
    }

    pub fn get_type_tok(
        &self,
        metadata_emit: &IMetaDataEmit2,
        cor_lib_assembly_ref: mdAssemblyRef,
    ) -> Result<mdToken, HRESULT> {
        let mut idx = 0;
        if self.data[idx] == CorElementType::ELEMENT_TYPE_BYREF as u8 {
            idx += 1;
        }

        let element_type = CorElementType::from_u8(self.data[idx]).unwrap();
        match element_type {
            CorElementType::ELEMENT_TYPE_BOOLEAN => {
                metadata_emit.define_type_ref_by_name(cor_lib_assembly_ref, "System.Boolean")
            }
            CorElementType::ELEMENT_TYPE_CHAR => {
                metadata_emit.define_type_ref_by_name(cor_lib_assembly_ref, "System.Char")
            }
            CorElementType::ELEMENT_TYPE_I1 => {
                metadata_emit.define_type_ref_by_name(cor_lib_assembly_ref, "System.SByte")
            }
            CorElementType::ELEMENT_TYPE_U1 => {
                metadata_emit.define_type_ref_by_name(cor_lib_assembly_ref, "System.Byte")
            }
            CorElementType::ELEMENT_TYPE_I2 => {
                metadata_emit.define_type_ref_by_name(cor_lib_assembly_ref, "System.Int16")
            }
            CorElementType::ELEMENT_TYPE_U2 => {
                metadata_emit.define_type_ref_by_name(cor_lib_assembly_ref, "System.UInt16")
            }
            CorElementType::ELEMENT_TYPE_I4 => {
                metadata_emit.define_type_ref_by_name(cor_lib_assembly_ref, "System.In32")
            }
            CorElementType::ELEMENT_TYPE_U4 => {
                metadata_emit.define_type_ref_by_name(cor_lib_assembly_ref, "System.UInt32")
            }
            CorElementType::ELEMENT_TYPE_I8 => {
                metadata_emit.define_type_ref_by_name(cor_lib_assembly_ref, "System.Int64")
            }
            CorElementType::ELEMENT_TYPE_U8 => {
                metadata_emit.define_type_ref_by_name(cor_lib_assembly_ref, "System.UInt64")
            }
            CorElementType::ELEMENT_TYPE_R4 => {
                metadata_emit.define_type_ref_by_name(cor_lib_assembly_ref, "System.Single")
            }
            CorElementType::ELEMENT_TYPE_R8 => {
                metadata_emit.define_type_ref_by_name(cor_lib_assembly_ref, "System.Double")
            }
            CorElementType::ELEMENT_TYPE_I => {
                metadata_emit.define_type_ref_by_name(cor_lib_assembly_ref, "System.IntPtr")
            }
            CorElementType::ELEMENT_TYPE_U => {
                metadata_emit.define_type_ref_by_name(cor_lib_assembly_ref, "System.UIntPtr")
            }
            CorElementType::ELEMENT_TYPE_STRING => {
                metadata_emit.define_type_ref_by_name(cor_lib_assembly_ref, "System.String")
            }
            CorElementType::ELEMENT_TYPE_OBJECT => {
                metadata_emit.define_type_ref_by_name(cor_lib_assembly_ref, "System.Object")
            }
            CorElementType::ELEMENT_TYPE_CLASS | CorElementType::ELEMENT_TYPE_VALUETYPE => {
                idx += 1;
                let (token, len) = uncompress_token(&self.data[idx..]);
                Ok(token)
            }
            CorElementType::ELEMENT_TYPE_GENERICINST
            | CorElementType::ELEMENT_TYPE_SZARRAY
            | CorElementType::ELEMENT_TYPE_MVAR
            | CorElementType::ELEMENT_TYPE_VAR => {
                metadata_emit.get_token_from_type_spec(&self.data[idx..])
            }
            _ => Ok(mdTokenNil),
        }
    }
}

#[derive(Debug)]
pub struct ParsedFunctionMethodSignature {
    pub type_arg_len: u8,
    pub arg_len: u8,
    pub ret_type: (usize, usize),
    pub args: Vec<(usize, usize)>,
    pub data: Vec<u8>,
}

impl ParsedFunctionMethodSignature {
    pub fn arguments(&self) -> Vec<FunctionMethodArgument> {
        self.args
            .iter()
            .map(|(s, e)| FunctionMethodArgument::new(&self.data[*s..*e]))
            .collect()
    }

    pub fn return_type(&self) -> FunctionMethodArgument {
        FunctionMethodArgument::new(&self.data[self.ret_type.0..self.ret_type.1])
    }
}

#[derive(Debug, Clone)]
pub struct TypeInfo {
    pub id: mdToken,
    pub name: String,
    pub type_spec: mdTypeSpec,
    pub token_type: CorTokenType,
    pub extends_from: Option<Box<TypeInfo>>,
    pub is_value_type: bool,
    pub is_generic: bool,
    pub parent_type: Option<Box<TypeInfo>>,
}

/// A .NET version
#[derive(Clone, Eq, Debug)]
#[repr(C)]
pub struct Version {
    pub major: u16,
    pub minor: u16,
    pub build: u16,
    pub revision: u16,
}

impl Version {
    pub(crate) const MAX: Version = Version {
        major: u16::MAX,
        minor: u16::MAX,
        build: u16::MAX,
        revision: u16::MAX,
    };

    pub(crate) const MIN: Version = Version {
        major: 0,
        minor: 0,
        build: 0,
        revision: 0,
    };

    pub const fn new(major: u16, minor: u16, build: u16, revision: u16) -> Self {
        Version {
            major,
            minor,
            build,
            revision,
        }
    }

    pub fn parse(version: &str, default_missing_value: u16) -> Result<Self, Error> {
        if version.is_empty() {
            return Err(Error::InvalidVersion);
        }

        let parts = version.split('.').collect::<Vec<&str>>();
        if parts.len() > 4 {
            return Err(Error::InvalidVersion);
        }

        let major = match parts[0] {
            "*" => u16::MAX,
            m => m.parse::<u16>().map_err(|_| Error::InvalidVersion)?,
        };

        let minor = if parts.len() > 1 {
            match parts[1] {
                "*" => u16::MAX,
                m => m.parse::<u16>().map_err(|_| Error::InvalidVersion)?,
            }
        } else {
            default_missing_value
        };
        let build = if parts.len() > 2 {
            match parts[2] {
                "*" => u16::MAX,
                m => m.parse::<u16>().map_err(|_| Error::InvalidVersion)?,
            }
        } else {
            default_missing_value
        };
        let revision = if parts.len() > 3 {
            match parts[3] {
                "*" => u16::MAX,
                m => m.parse::<u16>().map_err(|_| Error::InvalidVersion)?,
            }
        } else {
            default_missing_value
        };

        Ok(Version::new(major, minor, build, revision))
    }
}

impl FromStr for Version {
    type Err = Error;
    fn from_str(version: &str) -> Result<Self, Self::Err> {
        Self::parse(version, 0)
    }
}

impl PartialEq for Version {
    #[inline]
    fn eq(&self, other: &Self) -> bool {
        self.major == other.major
            && self.minor == other.minor
            && self.build == other.build
            && self.revision == other.revision
    }
}

impl PartialOrd for Version {
    fn partial_cmp(&self, other: &Version) -> Option<Ordering> {
        Some(self.cmp(other))
    }
}

impl Ord for Version {
    fn cmp(&self, other: &Version) -> Ordering {
        match self.major.cmp(&other.major) {
            Ordering::Equal => {}
            r => return r,
        }

        match self.minor.cmp(&other.minor) {
            Ordering::Equal => {}
            r => return r,
        }

        match self.build.cmp(&other.build) {
            Ordering::Equal => {}
            r => return r,
        }

        self.revision.cmp(&other.revision)
    }
}

impl Default for Version {
    fn default() -> Self {
        Version::new(0, 0, 0, 0)
    }
}

impl Display for Version {
    fn fmt(&self, f: &mut Formatter<'_>) -> fmt::Result {
        write!(
            f,
            "{}.{}.{}.{}",
            self.major, self.minor, self.build, self.revision
        )
    }
}

impl<'de> Deserialize<'de> for Version {
    fn deserialize<D>(deserializer: D) -> Result<Version, D::Error>
    where
        D: Deserializer<'de>,
    {
        deserialize_from_str(deserializer)
    }
}

/// Assembly public key
#[derive(Debug, Eq, PartialEq, Clone)]
pub struct PublicKey {
    bytes: Vec<u8>,
    hash_algorithm: Option<HashAlgorithmType>,
}

impl PublicKey {
    pub fn new(bytes: Vec<u8>, hash_algorithm: u32) -> Self {
        Self {
            bytes,
            hash_algorithm: HashAlgorithmType::from_u32(hash_algorithm),
        }
    }

    /// the public key bytes
    pub fn bytes(&self) -> &[u8] {
        self.bytes.as_slice()
    }

    /// the hash algorithm of the public key
    pub fn hash_algorithm(&self) -> Option<HashAlgorithmType> {
        self.hash_algorithm
    }

    /// the hex encoded public key
    pub fn public_key(&self) -> String {
        hex::encode(self.bytes())
    }

    /// the low 8 bytes of the SHA-1 hash of the originator’s public key in the assembly reference
    pub fn public_key_token(&self) -> String {
        if self.bytes.is_empty() {
            return String::new();
        }

        match &self.hash_algorithm {
            Some(HashAlgorithmType::Sha1) => {
                let mut sha1 = Sha1::new();
                sha1.input(&self.bytes);
                let mut buf: Vec<u8> = repeat(0).take((sha1.output_bits() + 7) / 8).collect();
                sha1.result(&mut buf);
                buf.reverse();
                hex::encode(buf[0..8].as_ref())
            }
            _ => String::new(),
        }
    }
}

#[derive(Debug)]
pub struct WrapperMethodRef {
    pub type_ref: mdTypeRef,
    pub method_ref: mdMemberRef,
}

#[repr(C)]
#[derive(Debug, Clone)]
pub struct AssemblyMetaData {
    pub name: String,
    pub locale: Option<String>,
    pub assembly_token: mdAssembly,
    pub public_key: PublicKey,
    pub version: Version,
    pub assembly_flags: CorAssemblyFlags,
}

#[derive(Debug, Eq, Copy, Clone, PartialEq, FromPrimitive)]
pub enum HashAlgorithmType {
    Md5 = 32771,
    None = 0,
    Sha1 = 32772,
    Sha256 = 32780,
    Sha384 = 32781,
    Sha512 = 32782,
}

bitflags! {
   pub struct MethodArgumentTypeFlag: u32 {
        const BY_REF = 1;
        const VOID = 2;
        const BOXED_TYPE = 4;
   }
}

#[cfg(test)]
pub mod tests {
    use crate::profiler::types::{
        AssemblyReference, Integration, MethodSignature, PublicKeyToken, Version,
    };
    use std::{error::Error, fs::File, io::BufReader, path::PathBuf};

    fn deserialize_and_assert(json: &str, expected: Version) -> Result<(), Box<dyn Error>> {
        let version: Version = serde_yaml::from_str(json)?;
        assert_eq!(expected, version);
        Ok(())
    }

    #[test]
    fn deserialize_version_with_major() -> Result<(), Box<dyn Error>> {
        deserialize_and_assert("\"5\"", Version::new(5, 0, 0, 0))
    }

    #[test]
    fn deserialize_version_with_major_minor() -> Result<(), Box<dyn Error>> {
        deserialize_and_assert("\"5.5\"", Version::new(5, 5, 0, 0))
    }

    #[test]
    fn deserialize_version_with_major_minor_build() -> Result<(), Box<dyn Error>> {
        deserialize_and_assert("\"5.5.5\"", Version::new(5, 5, 5, 0))
    }

    #[test]
    fn deserialize_version_with_major_minor_build_revision() -> Result<(), Box<dyn Error>> {
        deserialize_and_assert("\"5.5.5.5\"", Version::new(5, 5, 5, 5))
    }

    #[test]
    fn deserialize_version_with_major_stars() -> Result<(), Box<dyn Error>> {
        deserialize_and_assert("\"5.*.*.*\"", Version::new(5, u16::MAX, u16::MAX, u16::MAX))
    }

    #[test]
    fn deserialize_method_signature() -> Result<(), Box<dyn Error>> {
        let json = "\"00 08 1C 1C 1C 1C 1C 1C 08 08 0A\"";
        let method_signature: MethodSignature = serde_yaml::from_str(json)?;
        assert_eq!(
            MethodSignature {
                data: vec![0, 8, 28, 28, 28, 28, 28, 28, 8, 8, 10]
            },
            method_signature
        );
        Ok(())
    }

    #[test]
    fn deserialize_assembly_reference() -> Result<(), Box<dyn Error>> {
        let json =
            "\"Elastic.Apm, Version=1.9.0.0, Culture=neutral, PublicKeyToken=ae7400d2c189cf22\"";
        let assembly_reference: AssemblyReference = serde_yaml::from_str(json)?;

        let expected_assembly_reference = AssemblyReference {
            name: "Elastic.Apm".into(),
            version: Version::new(1, 9, 0, 0),
            locale: "neutral".into(),
            public_key: PublicKeyToken("ae7400d2c189cf22".into()),
        };

        assert_eq!(expected_assembly_reference, assembly_reference);
        Ok(())
    }

    #[test]
    fn deserialize_integration_from_yml() -> Result<(), Box<dyn Error>> {
        let json = r#"---
name: AdoNet
method_replacements:
- caller: {}
  target:
    assembly: System.Data
    type: System.Data.Common.DbCommand
    method: ExecuteNonQueryAsync
    signature_types:
    - System.Threading.Tasks.Task`1<System.Int32>
    - System.Threading.CancellationToken
    minimum_version: 4.0.0
    maximum_version: 4.*.*
  wrapper:
    assembly: Elastic.Apm.Profiler.Managed, Version=1.9.0.0, Culture=neutral, PublicKeyToken=ae7400d2c189cf22
    type: Elastic.Apm.Profiler.Integrations.AdoNet.CommandExecuteNonQueryAsyncIntegration
    action: CallTargetModification"#;

        let integration: Integration = serde_yaml::from_str(json)?;

        assert_eq!(&integration.name, "AdoNet");
        assert_eq!(integration.method_replacements.len(), 1);

        let method_replacement = &integration.method_replacements[0];

        assert!(method_replacement.caller.is_none());

        assert!(method_replacement.target.is_some());
        let target = method_replacement.target.as_ref().unwrap();
        assert_eq!(&target.assembly, "System.Data");
        assert_eq!(&target.type_name, "System.Data.Common.DbCommand");
        assert_eq!(&target.method_name, "ExecuteNonQueryAsync");
        assert_eq!(
            target
                .signature_types
                .as_ref()
                .unwrap()
                .iter()
                .map(String::as_str)
                .collect::<Vec<_>>(),
            vec![
                "System.Threading.Tasks.Task`1<System.Int32>",
                "System.Threading.CancellationToken"
            ]
        );
        assert_eq!(target.minimum_version, Version::new(4, 0, 0, 0));
        assert_eq!(
            target.maximum_version,
            Version::new(4, u16::MAX, u16::MAX, u16::MAX)
        );

        assert!(method_replacement.wrapper.is_some());
        let wrapper = method_replacement.wrapper.as_ref().unwrap();
        assert_eq!(
            &wrapper.type_name,
            "Elastic.Apm.Profiler.Integrations.AdoNet.CommandExecuteNonQueryAsyncIntegration"
        );
        assert_eq!(&wrapper.action, "CallTargetModification");

        Ok(())
    }

    #[test]
    fn deserialize_integrations_from_yml() -> Result<(), Box<dyn Error>> {
        let mut path = PathBuf::from(env!("CARGO_MANIFEST_DIR"));
        path.push("../Elastic.Apm.Profiler.Managed/integrations.yml");
        let file = File::open(path)?;
        let reader = BufReader::new(file);
        let integrations: Vec<Integration> = serde_yaml::from_reader(reader)?;
        assert!(integrations.len() > 0);
        Ok(())
    }

    #[test]
    fn public_key_token_into_bytes() {
        let public_key_token = PublicKeyToken::new("ae7400d2c189cf22");
        let bytes = public_key_token.into_bytes();
        assert_eq!(vec![174, 116, 0, 210, 193, 137, 207, 34], bytes);
    }
}
