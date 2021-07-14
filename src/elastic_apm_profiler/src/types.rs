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
    error::Error,
    ffi::{
        mdAssembly, mdMemberRef, mdMethodDef, mdToken, mdTypeDef, mdTypeRef, mdTypeSpec,
        AppDomainID, AssemblyID, ClassID, ClrInstanceID, CorAssemblyFlags, CorElementType,
        CorMethodAttr, CorMethodImpl, CorTokenType, CorTypeAttr, FunctionID, ModuleID, ProcessID,
        ReJITID, BYTE, COR_FIELD_OFFSET, COR_PRF_FRAME_INFO, COR_PRF_FUNCTION_ARGUMENT_INFO,
        COR_PRF_FUNCTION_ARGUMENT_RANGE, COR_PRF_HIGH_MONITOR, COR_PRF_MODULE_FLAGS,
        COR_PRF_MONITOR, COR_PRF_RUNTIME_TYPE, COR_SIGNATURE, DWORD, HCORENUM, LPCBYTE,
        PCCOR_SIGNATURE, ULONG, ULONG32,
    },
    interfaces::{ICorProfilerMethodEnum, IMetaDataImport},
    profiler::types::MethodSignature,
};

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

#[derive(Debug)]
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

#[derive(Debug)]
pub struct FunctionMethodSignature {
    pub data: Vec<COR_SIGNATURE>,
}

impl FunctionMethodSignature {
    pub fn new(data: Vec<COR_SIGNATURE>) -> Self {
        Self { data }
    }
}

#[derive(Debug)]
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
    const MAX: Version = Version {
        major: u16::MAX,
        minor: u16::MAX,
        build: u16::MAX,
        revision: u16::MAX,
    };

    const MIN: Version = Version {
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

    pub fn parse(version: &str) -> Result<Version, Error> {
        if version.is_empty() {
            return Err(Error::InvalidVersion);
        }

        let parts = version.split('.').collect::<Vec<&str>>();
        if parts.len() > 4 {
            return Err(Error::InvalidVersion);
        }
        let major = parts[0].parse::<u16>().map_err(|_| Error::InvalidVersion)?;
        let minor = if parts.len() > 1 {
            parts[1].parse::<u16>().map_err(|_| Error::InvalidVersion)?
        } else {
            0
        };
        let build = if parts.len() > 2 {
            parts[2].parse::<u16>().map_err(|_| Error::InvalidVersion)?
        } else {
            0
        };
        let revision = if parts.len() > 3 {
            parts[3].parse::<u16>().map_err(|_| Error::InvalidVersion)?
        } else {
            0
        };

        Ok(Version::new(major, minor, build, revision))
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

#[repr(C)]
#[derive(Debug)]
pub struct AssemblyMetaData {
    pub name: String,
    pub assembly_token: mdAssembly,
    pub public_key: PublicKey,
    pub version: Version,
    pub assembly_flags: CorAssemblyFlags,
}

/// Assembly public key
#[derive(Debug, Eq, PartialEq)]
pub struct PublicKey {
    pub bytes: Vec<u8>,
    pub hash_algorithm: Option<HashAlgorithmType>,
}

impl PublicKey {
    pub fn new(bytes: Vec<u8>, hash_algorithm: u32) -> Self {
        Self {
            bytes,
            hash_algorithm: HashAlgorithmType::from_u32(hash_algorithm),
        }
    }

    pub fn public_key(&self) -> String {
        hex::encode(self.bytes.as_slice())
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

#[derive(Debug, Eq, PartialEq, FromPrimitive)]
pub enum HashAlgorithmType {
    Md5 = 32771,
    None = 0,
    Sha1 = 32772,
    Sha256 = 32780,
    Sha384 = 32781,
    Sha512 = 32782,
}
