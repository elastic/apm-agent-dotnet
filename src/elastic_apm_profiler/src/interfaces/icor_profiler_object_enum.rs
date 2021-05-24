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
