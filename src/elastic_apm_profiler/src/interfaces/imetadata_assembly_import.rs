use crate::{ffi::*, types::*};
use com::{
    interfaces::iunknown::IUnknown,
    sys::{FAILED, HRESULT},
};
use core::slice;
use std::{ffi::c_void, mem::MaybeUninit, ptr};
use widestring::U16CString;

interfaces! {
    #[uuid("EE62470B-E94B-424E-9B7C-2F00C9249F93")]
    pub unsafe interface IMetaDataAssemblyImport: IUnknown {
        pub unsafe fn GetAssemblyProps(&self,
            mda: mdAssembly,
            ppbPublicKey: *mut *mut c_void,
            pcbPublicKey: *mut ULONG,
            pulHashAlgId: *mut ULONG,
            szName: *mut WCHAR,
            cchName: ULONG,
            pchName: *mut ULONG,
            pMetaData: *mut ASSEMBLYMETADATA,
            pdwAssemblyFlags: *mut DWORD,
        ) -> HRESULT;
        pub unsafe fn GetAssemblyRefProps(&self,
            mdar: mdAssemblyRef,
            ppbPublicKeyOrToken: *mut *mut c_void,
            pcbPublicKeyOrToken: *mut ULONG,
            szName: *mut WCHAR,
            cchName: ULONG,
            pchName: *mut ULONG,
            pMetaData: *mut ASSEMBLYMETADATA,
            ppbHashValue: *mut *mut c_void,
            pcbHashValue: *mut ULONG,
            pdwAssemblyRefFlags: *mut DWORD,
        ) -> HRESULT;
        pub unsafe fn GetFileProps(&self,
            mdf: mdFile,
            szName: *mut WCHAR,
            cchName: ULONG,
            pchName: *mut ULONG,
            ppbHashValue: *mut *mut c_void,
            pcbHashValue: *mut ULONG,
            pdwFileFlags: *mut DWORD,
        ) -> HRESULT;
        pub unsafe fn GetExportedTypeProps(&self,
            mdct: mdExportedType,
            szName: *mut WCHAR,
            cchName: ULONG,
            pchName: *mut ULONG,
            ptkImplementation: *mut mdToken,
            ptkTypeDef: *mut mdTypeDef,
            pdwExportedTypeFlags: *mut DWORD,
        ) -> HRESULT;
        pub unsafe fn GetManifestResourceProps(&self,
            mdmr: mdManifestResource,
            szName: *mut WCHAR,
            cchName: ULONG,
            pchName: *mut ULONG,
            ptkImplementation: *mut mdToken,
            pdwOffset: *mut DWORD,
            pdwResourceFlags: *mut DWORD,
        ) -> HRESULT;
        pub unsafe fn EnumAssemblyRefs(&self,
            phEnum: *mut HCORENUM,
            rAssemblyRefs: *mut mdAssemblyRef,
            cMax: ULONG,
            pcTokens: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumFiles(&self,
            phEnum: *mut HCORENUM,
            rFiles: *mut mdFile,
            cMax: ULONG,
            pcTokens: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumExportedTypes(&self,
            phEnum: *mut HCORENUM,
            rExportedTypes: *mut mdExportedType,
            cMax: ULONG,
            pcTokens: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumManifestResources(&self,
            phEnum: *mut HCORENUM,
            rManifestResources: *mut mdManifestResource,
            cMax: ULONG,
            pcTokens: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetAssemblyFromScope(&self, ptkAssembly: *mut mdAssembly) -> HRESULT;
        pub unsafe fn FindExportedTypeByName(&self,
            szName: LPCWSTR,
            mdtExportedType: mdToken,
            ptkExportedType: *mut mdExportedType,
        ) -> HRESULT;
        pub unsafe fn FindManifestResourceByName(&self,
            szName: LPCWSTR,
            ptkManifestResource: *mut mdManifestResource,
        ) -> HRESULT;
        pub unsafe fn CloseEnum(&self, hEnum: HCORENUM);
        pub unsafe fn FindAssembliesByName(&self,
            szAppBase: LPCWSTR,
            szPrivateBin: LPCWSTR,
            szAssemblyName: LPCWSTR,
            ppIUnk: *mut *mut IUnknown,
            cMax: ULONG,
            pcAssemblies: *mut ULONG,
        ) -> HRESULT;
    }
}

impl IMetaDataAssemblyImport {
    pub fn get_assembly_metadata(&self) -> Result<AssemblyMetaData, HRESULT> {
        let mut assembly_token = mdAssemblyNil;
        let hr = unsafe { self.GetAssemblyFromScope(&mut assembly_token) };

        if FAILED(hr) {
            log::error!("error calling assembly from scope");
            return Err(hr);
        }

        let mut name_buffer_length = MaybeUninit::uninit();
        unsafe {
            self.GetAssemblyProps(
                assembly_token,
                ptr::null_mut(),
                ptr::null_mut(),
                ptr::null_mut(),
                ptr::null_mut(),
                0,
                name_buffer_length.as_mut_ptr(),
                ptr::null_mut(),
                ptr::null_mut(),
            );
        }

        let name_buffer_length = unsafe { name_buffer_length.assume_init() };
        let mut name_buffer = Vec::<WCHAR>::with_capacity(name_buffer_length as usize);
        unsafe { name_buffer.set_len(name_buffer_length as usize) };

        let mut name_length = MaybeUninit::uninit();
        let mut assembly_metadata = MaybeUninit::uninit();
        let mut assembly_flags = MaybeUninit::uninit();
        let mut hash_algorithm = MaybeUninit::uninit();
        let mut public_key = MaybeUninit::uninit();
        let mut public_key_length = MaybeUninit::uninit();

        let hr = unsafe {
            self.GetAssemblyProps(
                assembly_token,
                public_key.as_mut_ptr(),
                public_key_length.as_mut_ptr(),
                hash_algorithm.as_mut_ptr(),
                name_buffer.as_mut_ptr(),
                name_buffer_length,
                name_length.as_mut_ptr(),
                assembly_metadata.as_mut_ptr(),
                assembly_flags.as_mut_ptr(),
            )
        };

        match hr {
            S_OK => {
                let name = U16CString::from_vec_with_nul(name_buffer)
                    .unwrap()
                    .to_string_lossy();

                let public_key = unsafe {
                    let l = public_key_length.assume_init();
                    let p = public_key.assume_init();
                    if l == 0 {
                        Vec::new()
                    } else {
                        slice::from_raw_parts(p as *const u8, l as usize).to_vec()
                    }
                };

                let hash_algorithm = unsafe { hash_algorithm.assume_init() };
                let assembly_metadata = unsafe { assembly_metadata.assume_init() };
                let assembly_flags = unsafe {
                    let a = assembly_flags.assume_init();
                    CorAssemblyFlags::from_bits(a).unwrap()
                };

                Ok(AssemblyMetaData {
                    name,
                    assembly_token,
                    public_key: PublicKey::new(public_key, hash_algorithm),
                    version: Version::new(
                        assembly_metadata.usMajorVersion,
                        assembly_metadata.usMinorVersion,
                        assembly_metadata.usBuildNumber,
                        assembly_metadata.usRevisionNumber,
                    ),
                    assembly_flags,
                })
            }
            _ => Err(hr),
        }
    }
}
