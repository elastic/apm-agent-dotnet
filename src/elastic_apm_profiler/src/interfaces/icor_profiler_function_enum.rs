// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::ffi::*;
use com::{interfaces::IUnknown, sys::HRESULT};

interfaces! {
    #[uuid("FF71301A-B994-429D-A10B-B345A65280EF")]
    pub unsafe interface ICorProfilerFunctionEnum: IUnknown {
        pub unsafe fn Skip(&self, celt: ULONG) -> HRESULT;
        pub unsafe fn Reset(&self) -> HRESULT;
        pub unsafe fn Clone(&self, ppEnum: *mut *mut ICorProfilerFunctionEnum) -> HRESULT;
        pub unsafe fn GetCount(&self, pcelt: *mut ULONG) -> HRESULT;
        pub unsafe fn Next(&self,
            celt: ULONG,
            ids: *mut COR_PRF_FUNCTION,
            pceltFetched: *mut ULONG,
        ) -> HRESULT;
    }

}
