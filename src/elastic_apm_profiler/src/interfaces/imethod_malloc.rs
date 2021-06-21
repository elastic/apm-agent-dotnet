use crate::ffi::*;
use c_vec::CVec;
use com::{interfaces, interfaces::IUnknown, sys::HRESULT};

interfaces! {
    #[uuid("A0EFB28B-6EE2-4D7B-B983-A75EF7BEEDB8")]
    pub unsafe interface IMethodMalloc: IUnknown {
        pub fn Alloc(&self, cb: ULONG) -> LPVOID;
    }
}

impl IMethodMalloc {
    pub fn alloc(&self, cb: ULONG) -> Result<CVec<u8>, HRESULT> {
        unsafe {
            let p = self.Alloc(cb);
            let address = p as *mut u8;
            if address.is_null() {
                log::error!("failed to allocate {} bytes", cb);
                return Err(E_FAIL);
            }

            Ok(CVec::new(address, cb as usize))
        }
    }
}
