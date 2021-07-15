use crate::{
    error::Error,
    ffi::{
        mdMemberRef, mdMemberRefNil, mdModule, mdTypeRef, AppDomainID, AssemblyID,
        CorAssemblyFlags, CorCallingConvention, CorElementType, ModuleID, ASSEMBLYMETADATA, BYTE,
        CLDB_E_RECORD_NOTFOUND, COR_PRF_MODULE_FLAGS, E_FAIL, LPCWSTR, ULONG, WCHAR,
    },
    interfaces::{
        IMetaDataAssemblyEmit, IMetaDataAssemblyImport, IMetaDataEmit2, IMetaDataImport2,
    },
    types::{MyFunctionInfo, PublicKey, Version, WrapperMethodRef},
};
use com::sys::{GUID, HRESULT};
use core::fmt;
use crypto::{digest::Digest, sha1::Sha1};
use num_traits::FromPrimitive;
use serde::{
    de,
    de::{DeserializeOwned, MapAccess, Visitor},
    Deserialize, Deserializer, Serialize,
};
use std::{
    collections::{BTreeMap, HashMap, HashSet},
    fmt::{Display, Formatter},
    fs::metadata,
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
        if self.data.len() > 1
            && self
                .calling_convention()
                .contains(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
            self.data[1]
        } else {
            0
        }
    }

    pub fn arguments_len(&self) -> u8 {
        if self.data.len() > 2
            && self
                .calling_convention()
                .contains(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
            self.data[2]
        } else if self.data.len() > 1 {
            self.data[1]
        } else {
            0
        }
    }

    pub fn return_type_is_object(&self) -> bool {
        if self.data.len() > 2
            && self
                .calling_convention()
                .contains(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
            CorElementType::from_u8(self.data[3]) == Some(CorElementType::ELEMENT_TYPE_OBJECT)
        } else if self.data.len() > 1 {
            CorElementType::from_u8(self.data[2]) == Some(CorElementType::ELEMENT_TYPE_OBJECT)
        } else {
            false
        }
    }

    pub fn index_of_return_type(&self) -> usize {
        if self.data.len() > 2
            && self
                .calling_convention()
                .contains(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
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
    name: String,
    version: Version,
    locale: String,
    public_key: PublicKeyToken,
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

        let version = Version::parse(map["Version"])?;
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

struct AssemblyReferenceVisitor;
impl<'de> Visitor<'de> for AssemblyReferenceVisitor {
    type Value = AssemblyReference;

    fn expecting(&self, formatter: &mut fmt::Formatter) -> fmt::Result {
        formatter.write_str("a string")
    }

    fn visit_str<E>(self, v: &str) -> Result<Self::Value, E>
    where
        E: de::Error,
    {
        AssemblyReference::from_str(v)
            .map_err(|_| de::Error::custom("Could not deserialize AssemblyReference"))
    }
}

impl<'de> Deserialize<'de> for AssemblyReference {
    fn deserialize<D>(deserializer: D) -> Result<AssemblyReference, D::Error>
    where
        D: Deserializer<'de>,
    {
        deserializer.deserialize_str(AssemblyReferenceVisitor)
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
    pub(crate) assembly: String,
    #[serde(rename = "type")]
    pub(crate) type_name: String,
    #[serde(rename = "method")]
    pub(crate) method_name: String,
    pub(crate) minimum_major: u16,
    pub(crate) minimum_minor: u16,
    pub(crate) minimum_patch: u16,
    #[serde(default = "u16_max")]
    pub(crate) maximum_major: u16,
    #[serde(default = "u16_max")]
    pub(crate) maximum_minor: u16,
    #[serde(default = "u16_max")]
    pub(crate) maximum_patch: u16,
    pub(crate) signature_types: Option<Vec<String>>,
}

fn u16_max() -> u16 {
    u16::MAX
}

impl TargetMethodReference {
    pub fn minimum_version(&self) -> Version {
        Version::new(
            self.minimum_major,
            self.minimum_minor,
            self.minimum_patch,
            0,
        )
    }

    pub fn maximum_version(&self) -> Version {
        Version::new(
            self.maximum_major,
            self.maximum_minor,
            self.maximum_patch,
            0,
        )
    }
}

#[derive(Debug, Eq, PartialEq, Deserialize, Clone)]
pub struct MethodReplacement {
    #[serde(deserialize_with = "empty_struct_is_none")]
    pub(crate) caller: Option<CallerMethodReference>,
    pub(crate) target: Option<TargetMethodReference>,
    pub(crate) wrapper: Option<WrapperMethodReference>,
}

fn empty_struct_is_none<'de, T, D>(deserializer: D) -> Result<Option<T>, D::Error>
where
    T: DeserializeOwned,
    D: Deserializer<'de>,
{
    #[derive(Deserialize)]
    #[serde(untagged)]
    enum DataOrEmpty<T> {
        Data(T),
        Empty {},
    }

    match DataOrEmpty::deserialize(deserializer)? {
        DataOrEmpty::Data(data) => Ok(Some(data)),
        DataOrEmpty::Empty {} => Ok(None),
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

pub struct ModuleMetadata {
    pub import: IMetaDataImport2,
    pub emit: IMetaDataEmit2,
    pub assembly_import: IMetaDataAssemblyImport,
    pub assembly_emit: IMetaDataAssemblyEmit,
    pub assembly_name: String,
    pub app_domain_id: AppDomainID,
    pub module_version_id: GUID,
    pub integrations: Vec<IntegrationMethod>,
    failed_wrapper_keys: HashSet<String>,
    wrapper_refs: HashMap<String, mdMemberRef>,
    wrapper_parent_type: HashMap<String, mdTypeRef>,
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

    pub fn get_method_replacements_for_caller(
        &self,
        caller: &MyFunctionInfo,
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
    module_metadata: &'a mut ModuleMetadata,
    module: mdModule,
}

impl<'a> MetadataBuilder<'a> {
    pub fn new(module_metadata: &'a mut ModuleMetadata, module: mdModule) -> Self {
        Self {
            module_metadata,
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
                assembly_metadata,
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
        if let Some(type_ref) = self.module_metadata.get_wrapper_parent_type_ref(&cache_key) {
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

        self.module_metadata
            .set_wrapper_parent_type_ref(cache_key, type_ref);
        Ok(type_ref)
    }

    pub fn store_wrapper_method_ref(
        &mut self,
        wrapper: &WrapperMethodReference,
    ) -> Result<(), HRESULT> {
        let cache_key = wrapper.get_method_cache_key();
        if self
            .module_metadata
            .contains_wrapper_member_ref(&cache_key)
        {
            return Ok(());
        }

        let type_ref = self.find_wrapper_type_ref(wrapper).map_err(|e| {
            log::warn!("failed finding wrapper method ref {}", e);
            self.module_metadata
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
                                        self.module_metadata
                                            .set_failed_wrapper_member_key(&cache_key);
                                        return Err(e);
                                    }
                                }
                            } else {
                                self.module_metadata
                                    .set_failed_wrapper_member_key(&cache_key);
                                return Err(e);
                            }
                        }
                    }
                }
            }
        }

        self.module_metadata
            .set_wrapper_member_ref(&cache_key, member_ref);
        Ok(())
    }
}

#[cfg(test)]
pub mod tests {
    use crate::{
        profiler::types::{AssemblyReference, Integration, MethodSignature, PublicKeyToken},
        types::Version,
    };
    use std::{error::Error, fs::File};

    #[test]
    fn deserialize_method_signature() -> Result<(), Box<dyn Error>> {
        let json = "\"00 08 1C 1C 1C 1C 1C 1C 08 08 0A\"";
        let method_signature: MethodSignature = serde_json::from_str(json)?;
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
        let assembly_reference: AssemblyReference = serde_json::from_str(json)?;

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
    fn deserialize_integrations() -> Result<(), Box<dyn Error>> {
        let json = r#"[
              {
                "name": "AdoNet",
                "method_replacements": [
                  {
                    "caller": {},
                    "target": {
                      "assembly": "System.Data",
                      "type": "System.Data.Common.DbCommand",
                      "method": "ExecuteNonQueryAsync",
                      "signature_types": [
                        "System.Threading.Tasks.Task`1<System.Int32>",
                        "System.Threading.CancellationToken"
                      ],
                      "minimum_major": 4,
                      "minimum_minor": 0,
                      "minimum_patch": 0,
                      "maximum_major": 4,
                      "maximum_minor": 65535,
                      "maximum_patch": 65535
                    },
                    "wrapper": {
                      "assembly": "Elastic.Apm.Profiler.Managed, Version=1.9.0.0, Culture=neutral, PublicKeyToken=ae7400d2c189cf22",
                      "type": "Elastic.Apm.Profiler.Integrations.AdoNet.CommandExecuteNonQueryAsyncIntegration",
                      "action": "CallTargetModification"
                    }
                  }
                ]
              },
              {
                "name": "AspNet",
                "method_replacements": [
                  {
                    "caller": {},
                    "target": {
                      "assembly": "System.Web",
                      "type": "System.Web.Compilation.BuildManager",
                      "method": "InvokePreStartInitMethodsCore",
                      "signature_types": [
                        "System.Void",
                        "System.Collections.Generic.ICollection`1[System.Reflection.MethodInfo]",
                        "System.Func`1[System.IDisposable]"
                      ],
                      "minimum_major": 4,
                      "minimum_minor": 0,
                      "minimum_patch": 0,
                      "maximum_major": 4,
                      "maximum_minor": 65535,
                      "maximum_patch": 65535
                    },
                    "wrapper": {
                      "assembly": "Elastic.Apm.Profiler.Managed, Version=1.9.0.0, Culture=neutral, PublicKeyToken=ae7400d2c189cf22",
                      "type": "Elastic.Apm.Profiler.Integrations.AspNet.HttpModule_Integration",
                      "action": "CallTargetModification"
                    }
                  }
                ]
              },
              {
                "name": "XUnit",
                "method_replacements": [
                  {
                    "caller": {},
                    "target": {
                      "assembly": "xunit.execution.desktop",
                      "type": "Xunit.Sdk.TestInvoker`1",
                      "method": "RunAsync",
                      "signature_types": [
                        "System.Threading.Tasks.Task`1<System.Decimal>"
                      ],
                      "minimum_major": 2,
                      "minimum_minor": 2,
                      "minimum_patch": 0,
                      "maximum_major": 2,
                      "maximum_minor": 65535,
                      "maximum_patch": 65535
                    },
                    "wrapper": {
                      "assembly": "Elastic.Apm.Profiler.Managed, Version=1.9.0.0, Culture=neutral, PublicKeyToken=ae7400d2c189cf22",
                      "type": "Elastic.Apm.Profiler.Integrations.Testing.XUnitIntegration",
                      "method": "TestInvoker_RunAsync",
                      "signature": "00 04 1C 1C 08 08 0A",
                      "action": "ReplaceTargetMethod"
                    }
                  }
                ]
              }
            ]"#;

        let integrations: Vec<Integration> = serde_json::from_str(json)?;

        assert_eq!(3, integrations.len());
        Ok(())
    }

    #[test]
    fn public_key_token_into_bytes() {
        let public_key_token = PublicKeyToken::new("ae7400d2c189cf22");
        let bytes = public_key_token.into_bytes();
        assert_eq!(vec![174, 116, 0, 210, 193, 137, 207, 34], bytes);
    }
}
