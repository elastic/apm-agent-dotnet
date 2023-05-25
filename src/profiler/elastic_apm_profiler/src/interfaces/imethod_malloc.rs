// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::ffi::*;
use c_vec::CVec;
use com::{interfaces, interfaces::IUnknown, sys::HRESULT};

interfaces! {
    /// /// Provides a method to allocate memory for a new Microsoft intermediate language (MSIL) function body.
    #[uuid("A0EFB28B-6EE2-4D7B-B983-A75EF7BEEDB8")]
    pub unsafe interface IMethodMalloc: IUnknown {
        /// Attempts to allocate a specified amount of memory for a new MSIL function body.
        pub fn Alloc(&self, cb: ULONG) -> LPVOID;
    }
}

impl IMethodMalloc {
    /// Attempts to allocate a specified amount of memory for a new MSIL function body.
    /// - cb: the number of bytes to allocate
    pub fn alloc(&self, cb: ULONG) -> Result<CVec<u8>, HRESULT> {
        unsafe {
            let p = self.Alloc(cb);
            if p.is_null() {
                log::error!("failed to allocate {} bytes", cb);
                Err(E_FAIL)
            } else {
                let address = p as *mut u8;
                Ok(CVec::new(address, cb as usize))
            }
        }
    }
}
