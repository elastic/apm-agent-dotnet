// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::ffi::*;
use com::{interfaces, interfaces::IUnknown, sys::HRESULT};

interfaces! {
    #[uuid("2C6269BD-2D13-4321-AE12-6686365FD6AF")]
    pub unsafe interface ICorProfilerObjectEnum: IUnknown {
        pub fn Skip(&self, celt: ULONG) -> HRESULT;
        pub fn Reset(&self) -> HRESULT;
        pub fn Clone(&self, ppEnum: *mut *mut ICorProfilerObjectEnum) -> HRESULT;
        pub fn GetCount(&self, pcelt: *mut ULONG) -> HRESULT;
        pub fn Next(&self,
            celt: ULONG,
            objects: *mut ObjectID,
            pceltFetched: *mut ULONG,
        ) -> HRESULT;
    }
}
