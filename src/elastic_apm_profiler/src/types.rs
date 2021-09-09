// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use std::{
    cmp::Ordering,
    fmt,
    fmt::{Display, Formatter},
    iter::repeat,
};

use com::{interfaces::iunknown::IUnknown, sys::GUID};
use crypto::{digest::Digest, sha1::Sha1};
use num_traits::FromPrimitive;

use crate::{
    cil::{uncompress_data, uncompress_token},
    error::Error,
    ffi::{
        mdAssembly, mdAssemblyRef, mdMemberRef, mdMethodDef, mdToken, mdTokenNil, mdTypeDef,
        mdTypeRef, mdTypeSpec, AppDomainID, AssemblyID, ClassID, ClrInstanceID, CorAssemblyFlags,
        CorCallingConvention, CorElementType, CorMethodAttr, CorMethodImpl, CorTokenType,
        CorTypeAttr, FunctionID, ModuleID, ProcessID, ReJITID, ASSEMBLYMETADATA, BYTE,
        COR_FIELD_OFFSET, COR_PRF_FRAME_INFO, COR_PRF_FUNCTION_ARGUMENT_INFO,
        COR_PRF_FUNCTION_ARGUMENT_RANGE, COR_PRF_HIGH_MONITOR, COR_PRF_MODULE_FLAGS,
        COR_PRF_MONITOR, COR_PRF_RUNTIME_TYPE, COR_SIGNATURE, DWORD, HCORENUM, LPCBYTE,
        PCCOR_SIGNATURE, ULONG, ULONG32,
    },
    interfaces::{ICorProfilerMethodEnum, IMetaDataEmit2, IMetaDataImport},
    profiler::types::{deserialize_from_str, MethodSignature},
};
use com::sys::HRESULT;
use serde::{de, de::Visitor, Deserialize, Deserializer};
use std::str::FromStr;

pub struct ArrayClassInfo {
    pub element_type: CorElementType,
    pub element_class_id: Option<ClassID>,
    pub rank: u32,
}
pub struct ClassInfo {
    pub module_id: ModuleID,
    pub token: mdTypeDef,
}
#[derive(Debug)]
pub struct FunctionInfo {
    pub class_id: ClassID,
    pub module_id: ModuleID,
    pub token: mdMethodDef,
}
pub struct FunctionTokenAndMetadata {
    pub metadata_import: *mut IUnknown,
    pub token: mdMethodDef,
}
#[derive(Debug)]
pub struct ModuleInfo {
    pub base_load_address: LPCBYTE,
    pub file_name: String,
    pub assembly_id: AssemblyID,
}
pub struct IlFunctionBody {
    pub method_header: LPCBYTE,
    pub method_size: u32,
}
impl From<IlFunctionBody> for &[u8] {
    fn from(body: IlFunctionBody) -> Self {
        unsafe { std::slice::from_raw_parts(body.method_header, body.method_size as usize) }
    }
}

pub struct AppDomainInfo {
    pub name: String,
    pub process_id: ProcessID,
}
pub struct AssemblyInfo {
    pub name: String,
    pub module_id: ModuleID,
    pub app_domain_id: AppDomainID,
}
pub struct FunctionInfo2 {
    pub class_id: ClassID,
    pub module_id: ModuleID,
    pub token: mdMethodDef,
    pub type_args: Vec<ClassID>,
}
pub struct ClassLayout {
    pub field_offset: Vec<COR_FIELD_OFFSET>,
    pub class_size_bytes: u32,
}
pub struct ClassInfo2 {
    pub module_id: ModuleID,
    pub token: mdTypeDef,
    pub parent_class_id: ClassID,
    pub type_args: Vec<ClassID>,
}
pub struct ArrayObjectInfo {
    pub dimension_sizes: Vec<u32>,
    pub dimension_lower_bounds: Vec<i32>,
    pub data: *mut BYTE,
}
pub struct StringLayout {
    pub string_length_offset: u32,
    pub buffer_offset: u32,
}
pub struct FunctionEnter3Info {
    pub frame_info: COR_PRF_FRAME_INFO,
    pub argument_info_length: u32,
    pub argument_info: COR_PRF_FUNCTION_ARGUMENT_INFO,
}
pub struct FunctionLeave3Info {
    pub frame_info: COR_PRF_FRAME_INFO,
    pub retval_range: COR_PRF_FUNCTION_ARGUMENT_RANGE,
}
pub struct RuntimeInfo {
    pub clr_instance_id: ClrInstanceID,
    pub runtime_type: COR_PRF_RUNTIME_TYPE,
    pub major_version: u16,
    pub minor_version: u16,
    pub build_number: u16,
    pub qfe_version: u16,
    pub version_string: String,
}
impl RuntimeInfo {
    fn runtime_name(&self) -> &str {
        match self.runtime_type {
            COR_PRF_RUNTIME_TYPE::COR_PRF_DESKTOP_CLR => "Desktop CLR",
            COR_PRF_RUNTIME_TYPE::COR_PRF_CORE_CLR => "Core CLR",
        }
    }

    pub fn is_desktop_clr(&self) -> bool {
        self.runtime_type == COR_PRF_RUNTIME_TYPE::COR_PRF_DESKTOP_CLR
    }

    pub fn is_core_clr(&self) -> bool {
        self.runtime_type == COR_PRF_RUNTIME_TYPE::COR_PRF_CORE_CLR
    }
}

impl Display for RuntimeInfo {
    fn fmt(&self, f: &mut Formatter<'_>) -> fmt::Result {
        write!(f, "{} {}", self.runtime_name(), &self.version_string)
    }
}

pub struct ModuleInfo2 {
    pub base_load_address: LPCBYTE,
    pub file_name: String,
    pub assembly_id: AssemblyID,
    pub module_flags: COR_PRF_MODULE_FLAGS,
}
impl ModuleInfo2 {
    pub fn is_windows_runtime(&self) -> bool {
        self.module_flags
            .contains(COR_PRF_MODULE_FLAGS::COR_PRF_MODULE_WINDOWS_RUNTIME)
    }
}
pub struct FunctionAndRejit {
    pub function_id: FunctionID,
    pub rejit_id: ReJITID,
}
pub struct EventMask2 {
    pub events_low: COR_PRF_MONITOR,
    pub events_high: COR_PRF_HIGH_MONITOR,
}
pub struct EnumNgenModuleMethodsInliningThisMethod<'a> {
    pub incomplete_data: bool,
    pub method_enum: &'a mut ICorProfilerMethodEnum,
}
pub struct DynamicFunctionInfo {
    pub module_id: ModuleID,
    pub sig: PCCOR_SIGNATURE,
    pub sig_length: u32,
    pub name: String,
}
pub struct MethodProps {
    pub class_token: mdTypeDef,
    pub name: String,
    pub attr_flags: CorMethodAttr,
    pub sig: PCCOR_SIGNATURE,
    pub sig_length: u32,
    pub rva: u32,
    pub impl_flags: CorMethodImpl,
}
pub struct ScopeProps {
    pub name: String,
    pub version: GUID,
}
pub struct TypeSpec {
    /// The type spec signature
    pub signature: Vec<COR_SIGNATURE>,
}
pub struct ModuleRefProps {
    /// The name of the referenced module
    pub name: String,
}
pub struct TypeDefProps {
    /// The type name
    pub name: String,
    /// Flags that modify the type definition
    pub cor_type_attr: CorTypeAttr,
    /// A TypeDef or TypeRef metadata token that represents the base type of the requested type.
    pub extends_td: mdToken,
}
pub struct TypeRefProps {
    /// The type name
    pub name: String,
    /// A pointer to the scope in which the reference is made. This value is an AssemblyRef or ModuleRef metadata token.
    pub parent_token: mdToken,
}
pub struct MemberRefProps {
    /// The type name
    pub name: String,
    pub class_token: mdToken,
    pub signature: Vec<COR_SIGNATURE>,
}
pub struct MemberProps {
    pub name: String,
    pub class_token: mdTypeDef,
    pub member_flags: DWORD,
    pub relative_virtual_address: ULONG,
    pub method_impl_flags: DWORD,
    pub element_type: CorElementType,
    pub signature: Vec<COR_SIGNATURE>,
    pub value: String,
}

pub struct MethodSpecProps {
    /// the MethodDef or MethodRef token that represents the method definition.
    pub parent: mdToken,
    /// the binary metadata signature of the method.
    pub signature: Vec<COR_SIGNATURE>,
}

#[derive(Debug)]
pub struct WrapperMethodRef {
    pub type_ref: mdTypeRef,
    pub method_ref: mdMemberRef,
}

#[derive(Debug, Clone)]
pub struct MyFunctionInfo {
    pub id: mdToken,
    pub name: String,
    pub type_info: Option<MyTypeInfo>,
    pub is_generic: bool,
    pub signature: MethodSignature,
    pub function_spec_signature: Option<MethodSignature>,
    pub method_def_id: mdToken,
    pub method_signature: FunctionMethodSignature,
}

impl MyFunctionInfo {
    pub fn new(
        id: mdToken,
        name: String,
        is_generic: bool,
        type_info: Option<MyTypeInfo>,
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

    fn parse_type_def_or_ref_encoded(signature: &[u8]) -> Option<(usize, usize)> {
        if let Some((token, len)) = uncompress_data(signature) {
            Some((0, len))
        } else {
            None
        }
    }

    fn parse_type(signature: &[u8]) -> Option<(usize, usize)> {
        let mut idx = 0;
        if let Some(elem_type) = CorElementType::from_u8(signature[idx]) {
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
                | CorElementType::ELEMENT_TYPE_OBJECT => Some((idx, idx + 1)),
                CorElementType::ELEMENT_TYPE_CLASS | CorElementType::ELEMENT_TYPE_VALUETYPE => {
                    idx += 1;
                    Self::parse_type_def_or_ref_encoded(&signature[idx..])
                        .map(|(s, e)| (0, idx + e))
                }
                CorElementType::ELEMENT_TYPE_SZARRAY => {
                    if signature.len() == 1 {
                        return None;
                    }

                    idx += 1;
                    if signature[idx] == CorElementType::ELEMENT_TYPE_CMOD_OPT as u8
                        || signature[idx] == CorElementType::ELEMENT_TYPE_CMOD_REQD as u8
                    {
                        None
                    } else {
                        Self::parse_type(&signature[idx..]).map(|(s, e)| (0, idx + e))
                    }
                }
                CorElementType::ELEMENT_TYPE_GENERICINST => {
                    if signature.len() == 1 {
                        return None;
                    }

                    idx += 1;
                    if signature[idx] != CorElementType::ELEMENT_TYPE_CLASS as u8
                        && signature[idx] != CorElementType::ELEMENT_TYPE_VALUETYPE as u8
                    {
                        return None;
                    }

                    if let Some((s, e)) = Self::parse_type_def_or_ref_encoded(&signature[idx..]) {
                        idx += e;
                    } else {
                        return None;
                    }

                    let num;
                    if let Some(num_idx) = crate::profiler::sig::parse_number(&signature[idx..]) {
                        idx += num_idx;
                        num = num_idx;
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

                    Some((0, idx))
                }
                CorElementType::ELEMENT_TYPE_VAR | CorElementType::ELEMENT_TYPE_MVAR => {
                    idx += 1;
                    uncompress_data(&signature[idx..]).map(|(data, len)| (0, idx + len))
                }
                _ => None,
            }
        } else {
            None
        }
    }

    fn parse_return_type(&self) -> Option<(usize, usize)> {
        let start_idx =
            if self.data.len() > 2 && self.calling_convention().map_or(false, |c| c.is_generic()) {
                3
            } else if self.data.len() > 1 {
                2
            } else {
                0
            };

        let mut idx = start_idx;

        if let Some(elem_type) = CorElementType::from_u8(self.data[idx]) {
            if elem_type == CorElementType::ELEMENT_TYPE_CMOD_OPT
                || elem_type == CorElementType::ELEMENT_TYPE_CMOD_REQD
            {
                return None;
            }

            if elem_type == CorElementType::ELEMENT_TYPE_TYPEDBYREF {
                return None;
            }

            if elem_type == CorElementType::ELEMENT_TYPE_VOID {
                return Some((start_idx, start_idx + 1));
            }

            if elem_type == CorElementType::ELEMENT_TYPE_BYREF {
                idx += 1;
            }

            Self::parse_type(&self.data[idx..]).map(|(_, end_idx)| (start_idx, start_idx + end_idx))
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

    fn type_arguments_len(&self) -> u8 {
        if self.data.len() > 1 && self.calling_convention().map_or(false, |c| c.is_generic()) {
            self.data[1]
        } else {
            0
        }
    }

    fn arguments_len(&self) -> u8 {
        if self.data.len() > 2 && self.calling_convention().map_or(false, |c| c.is_generic()) {
            self.data[2]
        } else if self.data.len() > 1 {
            self.data[1]
        } else {
            0
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
            let type_arg_len = self.type_arguments_len();
            let arg_len = self.arguments_len();
            let ret_type = match self.parse_return_type() {
                Some(r) => r,
                None => return None,
            };

            let mut sentinel_found = false;
            let mut idx = ret_type.1;
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

bitflags! {
   pub struct MethodArgumentTypeFlag: u32 {
        const BY_REF = 1;
        const VOID = 2;
        const BOXED_TYPE = 4;
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
pub struct MyTypeInfo {
    pub id: mdToken,
    pub name: String,
    pub type_spec: mdTypeSpec,
    pub token_type: CorTokenType,
    pub extends_from: Option<Box<MyTypeInfo>>,
    pub is_value_type: bool,
    pub is_generic: bool,
    pub parent_type: Option<Box<MyTypeInfo>>,
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
pub(crate) fn deserialize_max_version<'de, D>(deserializer: D) -> Result<Version, D::Error>
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

#[repr(C)]
#[derive(Debug, Clone)]
pub struct AssemblyMetaData {
    pub name: String,
    pub assembly_token: mdAssembly,
    pub public_key: PublicKey,
    pub version: Version,
    pub assembly_flags: CorAssemblyFlags,
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

#[derive(Debug, Eq, Copy, Clone, PartialEq, FromPrimitive)]
pub enum HashAlgorithmType {
    Md5 = 32771,
    None = 0,
    Sha1 = 32772,
    Sha256 = 32780,
    Sha384 = 32781,
    Sha512 = 32782,
}

#[cfg(test)]
pub mod tests {
    use crate::{
        profiler::types::{AssemblyReference, Integration, MethodSignature, PublicKeyToken},
        types::Version,
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
}
