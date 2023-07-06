// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::ffi::COR_PRF_ASSEMBLY_REFERENCE_INFO;
use com::{
    interfaces::iunknown::IUnknown,
    sys::{FAILED, HRESULT},
};

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

impl ICorProfilerAssemblyReferenceProvider {
    pub fn add_assembly_reference(
        &self,
        assembly_reference_info: &COR_PRF_ASSEMBLY_REFERENCE_INFO,
    ) -> Result<(), HRESULT> {
        let hr = unsafe { self.AddAssemblyReference(assembly_reference_info as *const _) };

        if FAILED(hr) {
            Err(hr)
        } else {
            Ok(())
        }
    }
}
