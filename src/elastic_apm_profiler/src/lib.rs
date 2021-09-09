#![allow(dead_code, unused_variables, unused_imports)]

// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#[macro_use]
extern crate bitflags;
#[macro_use]
extern crate com;
#[macro_use]
extern crate num_derive;

mod error;
mod ffi;
mod profiler;

pub mod cil;
pub mod interfaces;
pub mod types;

use com::sys::IID;
use profiler::Profiler;
use std::sync::atomic::Ordering;

/// The IID of the profiler
/// {FA65FE15-F085-4681-9B20-95E04F6C03CC}
const IID_PROFILER: IID = IID {
    data1: 0xFA65FE15,
    data2: 0xF085,
    data3: 0x4681,
    data4: [0x9B, 0x20, 0x95, 0xE0, 0x4F, 0x6C, 0x03, 0xCC],
};

/// Called by the runtime to get an instance of the profiler
#[no_mangle]
unsafe extern "system" fn DllGetClassObject(
    class_id: *const ::com::sys::CLSID,
    iid: *const ::com::sys::IID,
    result: *mut *mut ::core::ffi::c_void) -> ::com::sys::HRESULT {

    assert!(!class_id.is_null(), "class id passed to DllGetClassObject should never be null");
    let class_id = &*class_id;
    if class_id == &IID_PROFILER {
        let instance = <Profiler as ::com::production::Class>::Factory::allocate();
        instance.QueryInterface(&*iid, result)
    } else {
        ::com::sys::CLASS_E_CLASSNOTAVAILABLE
    }
}
