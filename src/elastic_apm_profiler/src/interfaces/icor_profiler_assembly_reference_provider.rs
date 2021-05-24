use crate::ffi::COR_PRF_ASSEMBLY_REFERENCE_INFO;
use com::{interfaces::iunknown::IUnknown, sys::HRESULT};

interfaces! {
    #[uuid("66A78C24-2EEF-4F65-B45F-DD1D8038BF3C")]
    pub unsafe interface ICorProfilerAssemblyReferenceProvider: IUnknown {
        pub fn AddAssemblyReference(
            &self,
            pAssemblyRefInfo: *const COR_PRF_ASSEMBLY_REFERENCE_INFO,
        ) -> HRESULT;
    }
}
