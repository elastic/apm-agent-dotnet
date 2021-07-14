// don't emit warnings for these for now.
#![allow(dead_code, unused_variables, unused_imports)]

#[macro_use]
extern crate bitflags;
#[macro_use]
extern crate com;
#[macro_use]
extern crate num_derive;

mod error;
mod ffi;
mod profiler;

pub mod cli;
pub mod interfaces;
pub mod types;

use com::sys::IID;
use profiler::Profiler;
use std::sync::atomic::Ordering;

/// The IID of the profiler
/// FA65FE15-F085-4681-9B20-95E04F6C03CC
pub const IID_PROFILER: IID = IID {
    data1: 0xFA65FE15,
    data2: 0xF085,
    data3: 0x4681,
    data4: [0x9B, 0x20, 0x95, 0xE0, 0x4F, 0x6C, 0x03, 0xCC],
};

macro_rules! dll_get_class_object {
    (($class_id_one:ident, $class_type_one:ty), $(($class_id:ident, $class_type:ty)),*) => {
        #[no_mangle]
        unsafe extern "system" fn DllGetClassObject(
            class_id: *const ::com::sys::CLSID,
            iid: *const ::com::sys::IID,
            result: *mut *mut ::core::ffi::c_void) -> ::com::sys::HRESULT {

            assert!(!class_id.is_null(), "class id passed to DllGetClassObject should never be null");
            let class_id = &*class_id;
            if class_id == &$class_id_one {
                let instance = <$class_type_one as ::com::production::Class>::Factory::allocate();
                instance.QueryInterface(&*iid, result)
            } $(else if class_id == &$class_id {
                let instance = <$class_type_one as ::com::production::Class>::Factory::allocate();
                instance.QueryInterface(&*iid, result)
            })* else {
                ::com::sys::CLASS_E_CLASSNOTAVAILABLE
            }
        }
    };
}

// associates Profiler with a clsid so that an instance
// can be created when the runtime asks for an instance by id when it calls DllGetClassObject
dll_get_class_object![(IID_PROFILER, Profiler),];
