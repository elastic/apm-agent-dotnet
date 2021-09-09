// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::{
    ffi::{
        mdMethodDef, mdToken, mdTypeDef, AppDomainID, AssemblyID, ClassID, ClrInstanceID,
        CorElementType, CorMethodAttr, CorMethodImpl, CorTypeAttr, FunctionID, ModuleID, ProcessID,
        ReJITID, BYTE, COR_FIELD_OFFSET, COR_PRF_FRAME_INFO, COR_PRF_FUNCTION_ARGUMENT_INFO,
        COR_PRF_FUNCTION_ARGUMENT_RANGE, COR_PRF_HIGH_MONITOR, COR_PRF_MODULE_FLAGS,
        COR_PRF_MONITOR, COR_PRF_RUNTIME_TYPE, COR_SIGNATURE, DWORD, LPCBYTE, PCCOR_SIGNATURE,
        ULONG,
    },
    interfaces::ICorProfilerMethodEnum,
};
use com::{interfaces::iunknown::IUnknown, sys::GUID};
use std::{
    fmt,
    fmt::{Display, Formatter},
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
