// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::profiler::{
    types::{AssemblyReference, PublicKeyToken},
    IS_ATTACHED, IS_DESKTOP_CLR,
};
use once_cell::sync::Lazy;
use rust_embed::RustEmbed;
use std::sync::atomic::Ordering;

/// Embedded assets of the managed loader assembly
#[derive(RustEmbed)]
#[folder = "../Elastic.Apm.Profiler.Managed.Loader/bin/Release"]
#[prefix = ""]
struct ManagedLoader;

pub const MANAGED_PROFILER_ASSEMBLY_LOADER: &str = "Elastic.Apm.Profiler.Managed.Loader";
pub const MANAGED_PROFILER_ASSEMBLY_LOADER_STARTUP: &str =
    "Elastic.Apm.Profiler.Managed.Loader.Startup";

pub const MANAGED_PROFILER_ASSEMBLY: &str = "Elastic.Apm.Profiler.Managed";

pub static MANAGED_PROFILER_FULL_ASSEMBLY_VERSION: Lazy<AssemblyReference> = Lazy::new(|| {
    AssemblyReference::new(
        MANAGED_PROFILER_ASSEMBLY,
        crate::profiler::PROFILER_VERSION.clone(),
        "neutral",
        PublicKeyToken::new("ae7400d2c189cf22"),
    )
});

pub const MANAGED_PROFILER_CALLTARGET_TYPE: &str =
    "Elastic.Apm.Profiler.Managed.CallTarget.CallTargetInvoker";
pub const MANAGED_PROFILER_CALLTARGET_BEGINMETHOD_NAME: &str = "BeginMethod";
pub const MANAGED_PROFILER_CALLTARGET_ENDMETHOD_NAME: &str = "EndMethod";
pub const MANAGED_PROFILER_CALLTARGET_LOGEXCEPTION_NAME: &str = "LogException";
pub const MANAGED_PROFILER_CALLTARGET_GETDEFAULTVALUE_NAME: &str = "GetDefaultValue";
pub const MANAGED_PROFILER_CALLTARGET_STATETYPE: &str =
    "Elastic.Apm.Profiler.Managed.CallTarget.CallTargetState";
pub const MANAGED_PROFILER_CALLTARGET_STATETYPE_GETDEFAULT_NAME: &str = "GetDefault";
pub const MANAGED_PROFILER_CALLTARGET_RETURNTYPE: &str =
    "Elastic.Apm.Profiler.Managed.CallTarget.CallTargetReturn";
pub const MANAGED_PROFILER_CALLTARGET_RETURNTYPE_GETDEFAULT_NAME: &str = "GetDefault";
pub const MANAGED_PROFILER_CALLTARGET_RETURNTYPE_GENERICS: &str =
    "Elastic.Apm.Profiler.Managed.CallTarget.CallTargetReturn`1";
pub const MANAGED_PROFILER_CALLTARGET_RETURNTYPE_GETRETURNVALUE_NAME: &str = "GetReturnValue";

pub const IGNORE: &str = "_";

/// Checks whether the profiler is attached.
#[no_mangle]
pub extern "C" fn IsProfilerAttached() -> bool {
    IS_ATTACHED.load(Ordering::SeqCst)
}

/// Gets the embedded loader assembly and symbol bytes
#[no_mangle]
pub extern "C" fn GetAssemblyAndSymbolsBytes(
    assembly: *mut *mut u8,
    assembly_size: &mut i32,
    symbols: *mut *mut u8,
    symbols_size: &mut i32,
) {
    let tfm = if IS_DESKTOP_CLR.load(Ordering::SeqCst) {
        "net461"
    } else {
        "netcoreapp2.0"
    };
    let a =
        ManagedLoader::get(&format!("{}/{}.dll", tfm, MANAGED_PROFILER_ASSEMBLY_LOADER)).unwrap();
    unsafe { *assembly = a.as_ptr() as *mut _ };
    *assembly_size = a.len() as i32;
    let s =
        ManagedLoader::get(&format!("{}/{}.pdb", tfm, MANAGED_PROFILER_ASSEMBLY_LOADER)).unwrap();
    unsafe { *symbols = s.as_ptr() as *mut _ };
    *symbols_size = s.len() as i32;
}
