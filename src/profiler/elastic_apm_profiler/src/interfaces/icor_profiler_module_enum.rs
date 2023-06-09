// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::ffi::*;
use com::{interfaces, interfaces::IUnknown, sys::HRESULT};

interfaces! {
    #[uuid("B0266D75-2081-4493-AF7F-028BA34DB891")]
    pub unsafe interface ICorProfilerModuleEnum: IUnknown {
        pub unsafe fn Skip(&self, celt: ULONG) -> HRESULT;
        pub unsafe fn Reset(&self) -> HRESULT;
        pub unsafe fn Clone(&self, ppEnum: *mut *mut ICorProfilerModuleEnum) -> HRESULT;
        pub unsafe fn GetCount(&self, pcelt: *mut ULONG) -> HRESULT;
        pub unsafe fn Next(&self,
            celt: ULONG,
            ids: *mut ModuleID,
            pceltFetched: *mut ULONG,
        ) -> HRESULT;
    }
}
