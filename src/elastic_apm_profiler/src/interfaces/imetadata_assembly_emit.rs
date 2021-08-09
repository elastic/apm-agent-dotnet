// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::ffi::*;
use com::{
    interfaces::iunknown::IUnknown,
    sys::{FAILED, HRESULT},
};

use std::{ffi::c_void, ptr};
use widestring::U16CString;

interfaces! {
    #[uuid("211EF15B-5317-4438-B196-DEC87B887693")]
    pub unsafe interface IMetaDataAssemblyEmit: IUnknown {
        pub unsafe fn DefineAssembly(&self,
            pbPublicKey: *const c_void,
            cbPublicKey: ULONG,
            ulHashAlgId: ULONG,
            szName: LPCWSTR,
            pMetaData: *const ASSEMBLYMETADATA,
            dwAssemblyFlags: DWORD,
            pmda: *mut mdAssembly,
        ) -> HRESULT;
        pub unsafe fn DefineAssemblyRef(&self,
            pbPublicKeyOrToken: *const c_void,
            cbPublicKeyOrToken: ULONG,
            szName: LPCWSTR,
            pMetaData: *const ASSEMBLYMETADATA,
            pbHashValue: *const c_void,
            cbHashValue: ULONG,
            dwAssemblyRefFlags: DWORD,
            pmdar: *mut mdAssemblyRef,
        ) -> HRESULT;
        pub unsafe fn DefineExportedType(&self,
            szName: LPCWSTR,
            tkImplementation: mdToken,
            tkTypeDef: mdTypeDef,
            dwExportedTypeFlags: DWORD,
            pmdct: *mut mdExportedType,
        ) -> HRESULT;
        pub unsafe fn DefineManifestResource(&self,
            szName: LPCWSTR,
            tkImplementation: mdToken,
            dwOffset: DWORD,
            dwResourceFlags: DWORD,
            pmdmr: *mut mdManifestResource,
        ) -> HRESULT;
        pub unsafe fn SetAssemblyProps(&self,
            pma: mdAssembly,
            pbPublicKey: *const c_void,
            cbPublicKey: ULONG,
            ulHashAlgId: ULONG,
            szName: LPCWSTR,
            pMetaData: *const ASSEMBLYMETADATA,
            dwAssemblyFlags: DWORD,
        ) -> HRESULT;
        pub unsafe fn SetAssemblyRefProps(&self,
            ar: mdAssemblyRef,
            pbPublicKeyOrToken: *const c_void,
            cbPublicKeyOrToken: ULONG,
            szName: LPCWSTR,
            pMetaData: *const ASSEMBLYMETADATA,
            pbHashValue: *const c_void,
            cbHashValue: ULONG,
            dwAssemblyRefFlags: DWORD,
        ) -> HRESULT;
        pub unsafe fn SetFileProps(&self,
            file: mdFile,
            pbHashValue: *const c_void,
            cbHashValue: ULONG,
            dwFileFlags: DWORD,
        ) -> HRESULT;
        pub unsafe fn SetExportedTypeProps(&self,
            ct: mdExportedType,
            tkImplementation: mdToken,
            tkTypeDef: mdTypeDef,
            dwExportedTypeFlags: DWORD,
        ) -> HRESULT;
        pub unsafe fn SetManifestResourceProps(&self,
            mr: mdManifestResource,
            tkImplementation: mdToken,
            dwOffset: DWORD,
            dwResourceFlags: DWORD,
        ) -> HRESULT;
    }
}

impl IMetaDataAssemblyEmit {
    /// Creates an AssemblyRef structure containing metadata for the assembly that this
    /// assembly references, and returns the associated metadata token.
    pub fn define_assembly_ref(
        &self,
        public_key: &[u8],
        name: &str,
        assembly_metadata: &ASSEMBLYMETADATA,
        hash: &[u8],
        assembly_ref_flags: CorAssemblyFlags,
    ) -> Result<mdAssemblyRef, HRESULT> {
        let key_ptr = public_key.as_ptr();
        let key_len = public_key.len() as ULONG;
        let wstr = U16CString::from_str(name).unwrap();
        let hash_ptr = if hash.is_empty() {
            ptr::null()
        } else {
            hash.as_ptr()
        };
        let hash_len = hash.len() as ULONG;
        let mut assembly_ref = mdAssemblyRefNil;
        let hr = unsafe {
            self.DefineAssemblyRef(
                key_ptr as *const c_void,
                key_len,
                wstr.as_ptr(),
                assembly_metadata as *const _,
                hash_ptr as *const c_void,
                hash_len,
                assembly_ref_flags.bits(),
                &mut assembly_ref,
            )
        };
        if FAILED(hr) {
            log::error!(
                "define assembly ref '{}' failed. HRESULT: {} {:X}",
                name,
                hr,
                hr
            );
            return Err(hr);
        }
        Ok(assembly_ref)
    }
}
