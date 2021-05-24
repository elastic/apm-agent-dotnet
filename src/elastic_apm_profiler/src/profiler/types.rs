use crate::ffi::{AppDomainID, AssemblyID, ModuleID, COR_PRF_MODULE_FLAGS, BYTE};
use crate::types::{Version, PublicKey};

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

#[derive(Debug, Eq, PartialEq)]
pub struct MethodSignature {
    data: Vec<BYTE>
}

#[derive(Debug, Eq, PartialEq)]
pub struct AssemblyReference {
    name: String,
    version: Version,
    locale: String,
    public_key: PublicKey,
}

#[derive(Debug, Eq, PartialEq)]
pub struct MethodReference {
    assembly: AssemblyReference,
    type_name: String,
    method_name: String,
    action: String,
    method_signature: MethodSignature,
    min_version: Version,
    max_version: Version,
    signature_types: Vec<String>
}

#[derive(Debug, Eq, PartialEq)]
pub struct MethodReplacement {
    caller_method: MethodReference,
    target_method: MethodReference,
    wrapper_method: MethodReference,
}

#[derive(Debug, Eq, PartialEq)]
pub struct IntegrationMethod {
    name: String,
    method_replacement: MethodReplacement
}

#[derive(Debug, Eq, PartialEq)]
pub struct Integration {
    name: String,
    method_replacements: Vec<MethodReplacement>
}
