use crate::ffi::COR_PRF_ASSEMBLY_REFERENCE_INFO;
use com::{interfaces::iunknown::IUnknown, sys::HRESULT};

interfaces! {
    /// Enables the profiler to inform the common language runtime (CLR) of assembly references
    /// that the profiler will add in the ICorProfilerCallback::ModuleLoadFinished callback.
    ///
    /// Supported in the .NET Framework 4.5.2 and later versions
    #[uuid("66A78C24-2EEF-4F65-B45F-DD1D8038BF3C")]
    pub unsafe interface ICorProfilerAssemblyReferenceProvider: IUnknown {
        /// Informs the CLR of an assembly reference that the profiler plans to add in
        /// the ModuleLoadFinished callback.
        pub fn AddAssemblyReference(
            &self,
            pAssemblyRefInfo: *const COR_PRF_ASSEMBLY_REFERENCE_INFO,
        ) -> HRESULT;
    }
}
