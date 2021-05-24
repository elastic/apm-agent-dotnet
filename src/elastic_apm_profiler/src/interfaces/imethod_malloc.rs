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

            // TODO: How is failure communicated?
            // Docs say If Alloc fails to allocate the requested number of bytes at an address
            // greater than the base address of the module, it returns E_OUTOFMEMORY, regardless
            // of the actual amount of memory space available.
            // https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/imethodmalloc-alloc-method
            let vec = CVec::new(address, cb as usize);
            Ok(vec)
        }
    }
}
