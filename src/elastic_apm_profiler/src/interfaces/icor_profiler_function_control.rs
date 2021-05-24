use crate::ffi::*;
use com::{
    interfaces::iunknown::IUnknown,
    sys::{FAILED, HRESULT},
};

interfaces! {
    #[uuid("F0963021-E1EA-4732-8581-E01B0BD3C0C6")]
    pub unsafe interface ICorProfilerFunctionControl: IUnknown {
        pub fn SetCodegenFlags(&self, flags: DWORD) -> HRESULT;
        pub fn SetILFunctionBody(
            &self,
            cbNewILMethodHeader: ULONG,
            pbNewILMethodHeader: LPCBYTE,
        ) -> HRESULT;
        pub fn SetILInstrumentedCodeMap(
            &self,
            cILMapEntries: ULONG,
            rgILMapEntries: *const COR_IL_MAP,
        ) -> HRESULT;
    }
}

impl ICorProfilerFunctionControl {
    pub fn set_il_function_body(
        &self,
        new_method_header_size: ULONG,
        new_method_header: LPCBYTE,
    ) -> Result<(), HRESULT> {
        let hr = unsafe { self.SetILFunctionBody(new_method_header_size, new_method_header) };
        if FAILED(hr) {
            Err(hr)
        } else {
            Ok(())
        }
    }
}
