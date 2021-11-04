// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
    pub fn set_il_function_body(&self, new_method: &[u8]) -> Result<(), HRESULT> {
        let len = new_method.len() as ULONG;
        let ptr = new_method.as_ptr();
        let hr = unsafe { self.SetILFunctionBody(len, ptr) };
        if FAILED(hr) {
            Err(hr)
        } else {
            Ok(())
        }
    }
}
