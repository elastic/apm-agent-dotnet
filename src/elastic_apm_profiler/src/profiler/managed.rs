use crate::profiler::{IS_ATTACHED, IS_DESKTOP_CLR};
use rust_embed::RustEmbed;
use std::{
    borrow::{Borrow, Cow},
    ops::Deref,
    sync::atomic::Ordering,
};

/// Embedded assets of the managed loader assembly
#[derive(RustEmbed)]
#[folder = "../Elastic.Apm.Profiler.Managed.Loader/bin/Release"]
#[prefix = ""]
struct ManagedLoader;

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
        ManagedLoader::get(&format!("{}/Elastic.Apm.Profiler.Managed.Loader.dll", tfm)).unwrap();
    unsafe { *assembly = a.as_ptr() as *mut _ };
    *assembly_size = a.len() as i32;
    let s =
        ManagedLoader::get(&format!("{}/Elastic.Apm.Profiler.Managed.Loader.pdb", tfm)).unwrap();
    unsafe { *symbols = s.as_ptr() as *mut _ };
    *symbols_size = s.len() as i32;
}
