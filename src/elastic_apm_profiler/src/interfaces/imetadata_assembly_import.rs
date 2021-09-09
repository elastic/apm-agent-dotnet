// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::{cil::MAX_LENGTH, ffi::*, ffi::types::*};
use com::{
    interfaces::iunknown::IUnknown,
    sys::{FAILED, HRESULT, S_OK},
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
        let hr = unsafe {
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
            )
        };

        if FAILED(hr) {
            return Err(hr);
        }

        let name_buffer_length = unsafe { name_buffer_length.assume_init() };
        let mut name_buffer = Vec::<WCHAR>::with_capacity(name_buffer_length as usize);
        unsafe { name_buffer.set_len(name_buffer_length as usize) };

        let mut name_length = MaybeUninit::uninit();
        // NOTE: null_mut() default values on ASSEMBLYMETADATA will not populated.
        // This is not an issue now, but would be if AssemblyMetaData were to expose these values
        let mut assembly_metadata = ASSEMBLYMETADATA::default();
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
                &mut assembly_metadata,
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

    pub fn enum_assembly_refs(&self) -> Result<Vec<mdAssemblyRef>, HRESULT> {
        let mut en = ptr::null_mut() as HCORENUM;
        let max = 256;
        let mut assembly_refs = Vec::with_capacity(max as usize);
        let mut assembly_len = MaybeUninit::uninit();

        let hr = unsafe {
            self.EnumAssemblyRefs(
                &mut en,
                assembly_refs.as_mut_ptr(),
                max,
                assembly_len.as_mut_ptr(),
            )
        };

        if FAILED(hr) {
            return Err(hr);
        }

        let len = unsafe {
            let len = assembly_len.assume_init();
            assembly_refs.set_len(len as usize);
            len
        };

        // no more assembly refs
        if len < max {
            unsafe {
                self.CloseEnum(en);
            }
            return Ok(assembly_refs);
        }

        let mut all_assembly_refs = assembly_refs;
        loop {
            assembly_refs = Vec::with_capacity(max as usize);
            assembly_len = MaybeUninit::uninit();
            let hr = unsafe {
                self.EnumAssemblyRefs(
                    &mut en,
                    assembly_refs.as_mut_ptr(),
                    max,
                    assembly_len.as_mut_ptr(),
                )
            };

            if FAILED(hr) {
                return Err(hr);
            }

            let len = unsafe {
                let len = assembly_len.assume_init();
                assembly_refs.set_len(len as usize);
                len
            };
            all_assembly_refs.append(&mut assembly_refs);
            if len < max {
                break;
            }
        }

        unsafe {
            self.CloseEnum(en);
        }
        Ok(all_assembly_refs)
    }

    pub fn get_referenced_assembly_metadata(
        &self,
        assembly_ref: mdAssemblyRef,
    ) -> Result<AssemblyMetaData, HRESULT> {
        let mut name_buffer = Vec::<WCHAR>::with_capacity(MAX_LENGTH as usize);
        let mut name_length = 0;
        let mut public_key = MaybeUninit::uninit();
        // NOTE: null_mut() default values on ASSEMBLYMETADATA will not populated.
        // This is not an issue now, but would be if AssemblyMetaData were to expose these values
        let mut assembly_metadata = ASSEMBLYMETADATA::default();
        let mut public_key_length = 0;
        let mut assembly_flags = 0;

        let hr = unsafe {
            self.GetAssemblyRefProps(
                assembly_ref,
                public_key.as_mut_ptr(),
                &mut public_key_length,
                name_buffer.as_mut_ptr(),
                MAX_LENGTH,
                &mut name_length,
                &mut assembly_metadata,
                ptr::null_mut(),
                ptr::null_mut(),
                &mut assembly_flags,
            )
        };

        if FAILED(hr) {
            return Err(hr);
        }

        unsafe { name_buffer.set_len(name_length as usize) };
        let name = U16CString::from_vec_with_nul(name_buffer)
            .unwrap()
            .to_string_lossy();

        let public_key = unsafe {
            let p = public_key.assume_init();
            if public_key_length == 0 {
                Vec::new()
            } else {
                slice::from_raw_parts(p as *const u8, public_key_length as usize).to_vec()
            }
        };

        let assembly_flags = CorAssemblyFlags::from_bits(assembly_flags).unwrap();

        Ok(AssemblyMetaData {
            name,
            assembly_token: assembly_ref,
            public_key: PublicKey::new(public_key, 32772),
            version: Version::new(
                assembly_metadata.usMajorVersion,
                assembly_metadata.usMinorVersion,
                assembly_metadata.usBuildNumber,
                assembly_metadata.usRevisionNumber,
            ),
            assembly_flags,
        })
    }

    // Other Rust abstractions

    pub fn find_assembly_ref(&self, name: &str) -> Option<mdAssemblyRef> {
        if let Ok(assembly_refs) = self.enum_assembly_refs() {
            for assembly_ref in assembly_refs.into_iter() {
                if let Ok(assembly_metadata) = self.get_referenced_assembly_metadata(assembly_ref) {
                    if assembly_metadata.name == name {
                        return Some(assembly_ref);
                    }
                }
            }
        }

        None
    }
}
