// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::ffi::*;
use com::{
    interfaces,
    interfaces::IUnknown,
    sys::{GUID, HRESULT},
};

com::interfaces! {
    #[uuid("571194F7-25ED-419F-AA8B-7016B3159701")]
    pub unsafe interface ICorProfilerThreadEnum: IUnknown {
    pub unsafe fn Skip(&self, celt: ULONG) -> HRESULT;
    pub unsafe fn Reset(&self) -> HRESULT;
    pub unsafe fn Clone(&self, ppEnum: *mut *mut ICorProfilerThreadEnum) -> HRESULT;
    pub unsafe fn GetCount(&self, pcelt: *mut ULONG) -> HRESULT;
    pub unsafe fn Next(&self,
        celt: ULONG,
        ids: *mut ThreadID,
        pceltFetched: *mut ULONG,
    ) -> HRESULT;
    }
}
