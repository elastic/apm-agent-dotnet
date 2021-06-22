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
                return Err(E_FAIL);
            } else {
                let address = p as *mut u8;
                Ok(CVec::new(address, cb as usize))
            }
        }
    }
}
