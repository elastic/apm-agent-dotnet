use crate::ffi::*;
use com::{interfaces::iunknown::IUnknown, sys::HRESULT};

interfaces! {
    /// Provides methods to sequentially iterate through a collection of modules loaded by
    /// the application or the profiler.
    #[uuid("FCCEE788-0088-454B-A811-C99F298D1942")]
    pub unsafe interface ICorProfilerMethodEnum: IUnknown {
        pub unsafe fn Skip(&self, celt: ULONG) -> HRESULT;
        pub unsafe fn Reset(&self) -> HRESULT;
        pub unsafe fn Clone(&self, ppEnum: *mut *mut ICorProfilerMethodEnum) -> HRESULT;
        pub unsafe fn GetCount(&self, pcelt: *mut ULONG) -> HRESULT;
        pub unsafe fn Next(&self,
            celt: ULONG,
            elements: *mut COR_PRF_METHOD,
            pceltFetched: *mut ULONG,
        ) -> HRESULT;
    }
}
