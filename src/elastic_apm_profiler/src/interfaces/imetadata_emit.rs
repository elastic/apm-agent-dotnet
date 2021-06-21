use crate::{
    ffi::*,
    interfaces::{
        imap_token::IMapToken, imetadata_assembly_emit::IMetaDataAssemblyEmit,
        imetadata_assembly_import::IMetaDataAssemblyImport, imetadata_import::IMetaDataImport,
        istream::IStream,
    },
};
use com::{interfaces::iunknown::IUnknown, sys::HRESULT};
use std::{
    ffi::{c_void, CString, OsStr, OsString},
    mem::MaybeUninit,
    ops::{Deref, DerefMut},
    pin::Pin,
    str::FromStr,
};
use widestring::{U16CStr, U16CString, U16String, WideCString, WideString};

interfaces! {
    #[uuid("BA3FEE4C-ECB9-4E41-83B7-183FA41CD859")]
    pub unsafe interface IMetaDataEmit: IUnknown {
        pub unsafe fn SetModuleProps(&self, szName: LPCWSTR) -> HRESULT;
        pub unsafe fn Save(&self, szName: LPCWSTR, dwSaveFlags: DWORD) -> HRESULT;
        pub unsafe fn SaveToStream(&self,
            pIStream: *const IStream,
            dwSaveFlags: DWORD,
        ) -> HRESULT;
        pub unsafe fn GetSaveSize(&self, fSave: CorSaveSize, pdwSaveSize: *mut DWORD) -> HRESULT;
        pub unsafe fn DefineTypeDef(&self,
            szTypeDef: LPCWSTR,
            dwTypeDefFlags: DWORD,
            tkExtends: mdToken,
            rtkImplements: *const mdToken,
            ptd: *mut mdTypeDef,
        ) -> HRESULT;
        pub unsafe fn DefineNestedType(&self,
            szTypeDef: LPCWSTR,
            dwTypeDefFlags: DWORD,
            tkExtends: mdToken,
            rtkImplements: *const mdToken,
            tdEncloser: mdTypeDef,
            ptd: *mut mdTypeDef,
        ) -> HRESULT;
        pub unsafe fn SetHandler(&self, pUnk: *const IUnknown) -> HRESULT;
        pub unsafe fn DefineMethod(&self,
            td: mdTypeDef,
            szName: LPCWSTR,
            dwMethodFlags: DWORD,
            pvSigBlob: PCCOR_SIGNATURE,
            cbSigBlob: ULONG,
            ulCodeRVA: ULONG,
            dwImplFlags: DWORD,
            pmd: *mut mdMethodDef,
        ) -> HRESULT;
        pub unsafe fn DefineMethodImpl(&self,
            td: mdTypeDef,
            tkBody: mdToken,
            tkDecl: mdToken,
        ) -> HRESULT;
        pub unsafe fn DefineTypeRefByName(&self,
            tkResolutionScope: mdToken,
            szName: LPCWSTR,
            ptr: *mut mdTypeRef,
        ) -> HRESULT;
        pub unsafe fn DefineImportType(&self,
            pAssemImport: *const IMetaDataAssemblyImport,
            pbHashValue: *const c_void,
            cbHashValue: ULONG,
            pImport: *const IMetaDataImport,
            tdImport: mdTypeDef,
            pAssemEmit: *const IMetaDataAssemblyEmit,
            ptr: *mut mdTypeRef,
        ) -> HRESULT;
        pub unsafe fn DefineMemberRef(&self,
            tkImport: mdToken,
            szName: LPCWSTR,
            pvSigBlob: PCCOR_SIGNATURE,
            cbSigBlob: ULONG,
            pmr: *mut mdMemberRef,
        ) -> HRESULT;
        pub unsafe fn DefineImportMember(&self,
            pAssemImport: *const IMetaDataAssemblyImport,
            pbHashValue: *const c_void,
            cbHashValue: ULONG,
            pImport: *const IMetaDataImport,
            mbMember: mdToken,
            pAssemEmit: *const IMetaDataAssemblyEmit,
            tkParent: mdToken,
            pmr: *mut mdMemberRef,
        ) -> HRESULT;
        pub unsafe fn DefineEvent(&self,
            td: mdTypeDef,
            szEvent: LPCWSTR,
            dwEventFlags: DWORD,
            tkEventType: mdToken,
            mdAddOn: mdMethodDef,
            mdRemoveOn: mdMethodDef,
            mdFire: mdMethodDef,
            rmdOtherMethods: *const mdMethodDef,
            pmdEvent: *mut mdEvent,
        ) -> HRESULT;
        pub unsafe fn SetClassLayout(&self,
            td: mdTypeDef,
            dwPackSize: DWORD,
            rFieldOffsets: *const COR_FIELD_OFFSET,
            ulClassSize: ULONG,
        ) -> HRESULT;
        pub unsafe fn DeleteClassLayout(&self, td: mdTypeDef) -> HRESULT;
        pub unsafe fn SetFieldMarshal(&self,
            tk: mdToken,
            pvNativeType: PCCOR_SIGNATURE,
            cbNativeType: ULONG) -> HRESULT;
        pub unsafe fn DeleteFieldMarshal(&self, tk: mdToken) -> HRESULT;
        pub unsafe fn DefinePermissionSet(&self,
            tk: mdToken,
            dwAction: DWORD,
            pvPermission: *const c_void,
            cbPermission: ULONG,
            ppm: *mut mdPermission,
        ) -> HRESULT;
        pub unsafe fn SetRVA(&self, md: mdMethodDef, ulRVA: ULONG) -> HRESULT;
        pub unsafe fn GetTokenFromSig(&self,
            pvSig: PCCOR_SIGNATURE,
            cbSig: ULONG,
            pmsig: *mut mdSignature,
        ) -> HRESULT;
        pub unsafe fn DefineModuleRef(&self, szName: LPCWSTR, pmur: *mut mdModuleRef) -> HRESULT;
        pub unsafe fn SetParent(&self, mr: mdMemberRef, tk: mdToken) -> HRESULT;
        pub unsafe fn GetTokenFromTypeSpec(&self,
            pvSig: PCCOR_SIGNATURE,
            cbSig: ULONG,
            ptypespec: *mut mdTypeSpec,
        ) -> HRESULT;
        pub unsafe fn SaveToMemory(&self, pbData: *mut c_void, cbData: ULONG) -> HRESULT;
        /// Gets a metadata token for the specified literal string.
        pub unsafe fn DefineUserString(&self,
            szString: LPCWSTR,
            cchString: ULONG,
            pstk: *mut mdString,
        ) -> HRESULT;
        pub unsafe fn DeleteToken(&self, tkObj: mdToken) -> HRESULT;
        pub unsafe fn SetMethodProps(&self,
            md: mdMethodDef,
            dwMethodFlags: DWORD,
            ulCodeRVA: ULONG,
            dwImplFlags: DWORD) -> HRESULT;
        pub unsafe fn SetTypeDefProps(&self,
            td: mdTypeDef,
            dwTypeDefFlags: DWORD,
            tkExtends: mdToken,
            rtkImplements: *const mdToken,
        ) -> HRESULT;
        pub unsafe fn SetEventProps(&self,
            ev: mdEvent,
            dwEventFlags: DWORD,
            tkEventType: mdToken,
            mdAddOn: mdMethodDef,
            mdRemoveOn: mdMethodDef,
            mdFire: mdMethodDef,
            rmdOtherMethods: *const mdMethodDef,
        ) -> HRESULT;
        pub unsafe fn SetPermissionSetProps(&self,
            tk: mdToken,
            dwAction: DWORD,
            pvPermission: *const c_void,
            cbPermission: ULONG,
            ppm: *mut mdPermission,
        ) -> HRESULT;
        pub unsafe fn DefinePinvokeMap(&self,
            tk: mdToken,
            dwMappingFlags: DWORD,
            szImportName: LPCWSTR,
            mrImportDLL: mdModuleRef,
        ) -> HRESULT;
        pub unsafe fn SetPinvokeMap(&self,
            tk: mdToken,
            dwMappingFlags: DWORD,
            szImportName: LPCWSTR,
            mrImportDLL: mdModuleRef,
        ) -> HRESULT;
        pub unsafe fn DeletePinvokeMap(&self, tk: mdToken) -> HRESULT;
        pub unsafe fn DefineCustomAttribute(&self,
            tkOwner: mdToken,
            tkCtor: mdToken,
            pCustomAttribute: *const c_void,
            cbCustomAttribute: ULONG,
            pcv: *mut mdCustomAttribute,
        ) -> HRESULT;
        pub unsafe fn SetCustomAttributeValue(&self,
            pcv: mdCustomAttribute,
            pCustomAttribute: *const c_void,
            cbCustomAttribute: ULONG,
        ) -> HRESULT;
        pub unsafe fn DefineField(&self,
            td: mdTypeDef,
            szName: LPCWSTR,
            dwFieldFlags: DWORD,
            pvSigBlob: PCCOR_SIGNATURE,
            cbSigBlob: ULONG,
            dwCPlusTypeFlag: DWORD,
            pValue: *const c_void,
            cchValue: ULONG,
            pmd: *mut mdFieldDef,
        ) -> HRESULT;
        pub unsafe fn DefineProperty(&self,
            td: mdTypeDef,
            szProperty: LPCWSTR,
            dwPropFlags: DWORD,
            pvSig: PCCOR_SIGNATURE,
            cbSig: ULONG,
            dwCPlusTypeFlag: DWORD,
            pValue: *const c_void,
            cchValue: ULONG,
            mdSetter: mdMethodDef,
            mdGetter: mdMethodDef,
            rmdOtherMethods: *const mdMethodDef,
            pmdProp: *mut mdProperty,
        ) -> HRESULT;
        pub unsafe fn DefineParam(&self,
            md: mdMethodDef,
            ulParamSeq: ULONG,
            szName: LPCWSTR,
            dwParamFlags: DWORD,
            dwCPlusTypeFlag: DWORD,
            pValue: *const c_void,
            cchValue: ULONG,
            ppd: *mut mdParamDef,
        ) -> HRESULT;
        pub unsafe fn SetFieldProps(&self,
            fd: mdFieldDef,
            dwFieldFlags: DWORD,
            dwCPlusTypeFlag: DWORD,
            pValue: *const c_void,
            cchValue: ULONG,
        ) -> HRESULT;
        pub unsafe fn SetPropertyProps(&self,
            pr: mdProperty,
            dwPropFlags: DWORD,
            dwCPlusTypeFlag: DWORD,
            pValue: *const c_void,
            cchValue: ULONG,
            mdSetter: mdMethodDef,
            mdGetter: mdMethodDef,
            rmdOtherMethods: *const mdMethodDef,
        ) -> HRESULT;
        pub unsafe fn SetParamProps(&self,
            pd: mdParamDef,
            szName: LPCWSTR,
            dwParamFlags: DWORD,
            dwCPlusTypeFlag: DWORD,
            pValue: *mut c_void,
            cchValue: ULONG,
        ) -> HRESULT;
        pub unsafe fn DefineSecurityAttributeSet(&self,
            tkObj: mdToken,
            rSecAttrs: *const COR_SECATTR,
            cSecAttrs: ULONG,
            pulErrorAttr: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn ApplyEditAndContinue(&self,
            pImport: *const IUnknown,
        ) -> HRESULT;
        pub unsafe fn TranslateSigWithScope(&self,
            pAssemImport: *const IMetaDataAssemblyImport,
            pbHashValue: *const c_void,
            cbHashValue: ULONG,
            import: *const IMetaDataImport,
            pbSigBlob: PCCOR_SIGNATURE,
            cbSigBlob: ULONG,
            pAssemEmit: *const IMetaDataAssemblyEmit,
            emit: *const IMetaDataEmit,
            pvTranslatedSig: PCOR_SIGNATURE,
            cbTranslatedSigMax: ULONG,
            pcbTranslatedSig: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn SetMethodImplFlags(&self, md: mdMethodDef, dwImplFlags: DWORD) -> HRESULT;
        pub unsafe fn SetFieldRVA(&self, fd: mdFieldDef, ulRVA: ULONG) -> HRESULT;
        pub unsafe fn Merge(&self,
            pImport: *const IMetaDataImport,
            pHostMapToken: *const IMapToken,
            pHandler: *const IUnknown,
        ) -> HRESULT;
        pub unsafe fn MergeEnd(&self) -> HRESULT;
    }

    #[uuid("F5DD9950-F693-42e6-830E-7B833E8146A9")]
    pub unsafe interface IMetaDataEmit2: IMetaDataEmit {
        pub unsafe fn DefineMethodSpec(&self,
            tkParent: mdToken,
            pvSigBlob: PCCOR_SIGNATURE,
            cbSigBlob: ULONG,
            pmi: *mut mdMethodSpec,
        ) -> HRESULT;
        pub unsafe fn GetDeltaSaveSize(&self, fSave: CorSaveSize, pdwSaveSize: *mut DWORD) -> HRESULT;
        pub unsafe fn SaveDelta(&self, szFile: LPCWSTR, dwSaveFlags: DWORD) -> HRESULT;
        pub unsafe fn SaveDeltaToStream(&self,
            pIStream: *const IStream,
            dwSaveFlags: DWORD,
        ) -> HRESULT;
        pub unsafe fn SaveDeltaToMemory(&self, pbData: *mut c_void, cbData: ULONG) -> HRESULT;
        pub unsafe fn DefineGenericParam(&self,
            tk: mdToken,
            ulParamSeq: ULONG,
            dwParamFlags: DWORD,
            szname: LPCWSTR,
            reserved: DWORD,
            rtkConstraints: *const mdToken,
            pgp: *mut mdGenericParam,
        ) -> HRESULT;
        pub unsafe fn SetGenericParamProps(&self,
            gp: mdGenericParam,
            dwParamFlags: DWORD,
            szName: LPCWSTR,
            reserved: DWORD,
            rtkConstraints: *const mdToken,
        ) -> HRESULT;
        pub unsafe fn ResetENCLog(&self) -> HRESULT;
    }
}

impl IMetaDataEmit {
    pub fn define_field(
        &self,
        type_def: mdTypeDef,
        name: &str,
        flags: CorFieldAttr,
        sig: &[COR_SIGNATURE],
        constant_flag: CorElementType,
        constant_value: Option<DWORD>,
        constant_value_len: ULONG,
    ) -> Result<mdFieldDef, HRESULT> {
        let wstr = U16CString::from_str(name).unwrap();
        let ptr = sig.as_ptr();
        let len = sig.len() as ULONG;

        let value = match constant_value {
            Some(v) => &v,
            None => std::ptr::null(),
        };
        let mut field_def = mdFieldDefNil;
        let hr = unsafe {
            self.DefineField(
                type_def,
                wstr.as_ptr(),
                flags.bits(),
                ptr,
                len,
                constant_flag as u32,
                value as *const _,
                constant_value_len,
                &mut field_def,
            )
        };
        match hr {
            S_OK => Ok(field_def),
            _ => Err(hr),
        }
    }

    /// Updates references to a module defined by a prior call to
    /// [crate::interfaces::imetadata_emit::IMetaDataEmit::DefineModuleRef].
    pub fn set_module_props(&self, name: &str) -> Result<(), HRESULT> {
        let wstr = U16CString::from_str(name).unwrap();
        let hr = unsafe { self.SetModuleProps(wstr.as_ptr()) };
        match hr {
            S_OK => Ok(()),
            _ => Err(hr),
        }
    }

    /// Defines a reference to a member of a module outside the current scope,
    /// and gets a token to that reference definition.
    /// - token
    ///
    ///   Token for the target member's class or interface, if the member is not global;
    ///   if the member is global, the mdModuleRef token for that other file.
    /// - name
    ///
    ///   The name of the target member
    /// - sig
    ///
    ///   The signature of the target member.
    pub fn define_member_ref(
        &self,
        token: mdToken,
        name: &str,
        sig: &[COR_SIGNATURE],
    ) -> Result<mdMemberRef, HRESULT> {
        let wstr = U16CString::from_str(name).unwrap();
        let mut member_ref = mdMemberRefNil;
        let ptr = sig.as_ptr();
        let len = sig.len() as ULONG;
        let hr = unsafe { self.DefineMemberRef(token, wstr.as_ptr(), ptr, len, &mut member_ref) };
        match hr {
            S_OK => Ok(member_ref),
            _ => Err(hr),
        }
    }

    /// Creates a definition for a method or global function with the specified signature,
    /// and returns a token to that method definition.
    /// - token
    ///
    ///   The token of the parent class or parent interface of the method.
    ///   Set to mdTokenNil, if defining a global function.
    /// - name
    ///
    ///   The member name
    /// - attributes
    ///
    ///   The attributes of the method or global function
    /// - sig
    ///
    ///   The method signature
    /// - address
    ///
    ///   The address of the code
    /// - implementation
    ///
    ///   The implementation features of the method
    pub fn define_method(
        &self,
        token: mdTypeDef,
        name: &str,
        attributes: CorMethodAttr,
        sig: &[COR_SIGNATURE],
        address: ULONG,
        implementation: CorMethodImpl,
    ) -> Result<mdMethodDef, HRESULT> {
        let wstr = U16CString::from_str(name).unwrap();
        let ptr = sig.as_ptr();
        let len = sig.len() as ULONG;
        let mut method_def = mdMethodDefNil;
        let hr = unsafe {
            self.DefineMethod(
                token,
                wstr.as_ptr(),
                attributes.bits(),
                ptr,
                len,
                address,
                implementation.bits(),
                &mut method_def,
            )
        };
        match hr {
            S_OK => Ok(method_def),
            _ => Err(hr),
        }
    }

    /// Creates the metadata signature for a module with the specified name.
    /// - name
    ///
    ///   The name of the other metadata file, typically a DLL. This is the file name only.
    ///   Do not use a full path name.
    pub fn define_module_ref(&self, name: &str) -> Result<mdModuleRef, HRESULT> {
        let wstr = U16CString::from_str(name).unwrap();
        let mut module_ref = mdModuleRefNil;
        let hr = unsafe { self.DefineModuleRef(wstr.as_ptr(), &mut module_ref) };
        match hr {
            S_OK => Ok(module_ref),
            _ => Err(hr),
        }
    }

    /// Sets features of the PInvoke signature of the method referenced by the specified token.
    pub fn define_pinvoke_map(
        &self,
        token: mdToken,
        flags: CorPinvokeMap,
        import_name: &str,
        import_dll: mdModuleRef,
    ) -> Result<(), HRESULT> {
        let wstr = U16CString::from_str(import_name).unwrap();
        let hr = unsafe { self.DefinePinvokeMap(token, flags.bits(), wstr.as_ptr(), import_dll) };
        match hr {
            S_OK => Ok(()),
            _ => Err(hr),
        }
    }

    pub fn define_type_def(
        &self,
        name: &str,
        flags: CorTypeAttr,
        extends: mdToken,
        implements: Option<mdToken>,
    ) -> Result<mdTypeDef, HRESULT> {
        let wstr = U16CString::from_str(name).unwrap();
        let implements: *const mdTypeDef = match implements {
            Some(v) => &v,
            None => std::ptr::null(),
        };
        let mut type_def = mdTypeDefNil;
        let hr = unsafe {
            self.DefineTypeDef(
                wstr.as_ptr(),
                flags.bits(),
                extends,
                implements,
                &mut type_def,
            )
        };

        match hr {
            S_OK => Ok(type_def),
            _ => Err(hr),
        }
    }

    /// Gets a metadata token for a type that is defined in the specified scope,
    /// which is outside the current scope.
    /// - token
    ///   The token specifying the resolution scope. The following token types are valid:
    ///   - mdModuleRef
    ///     if the type is defined in the same assembly in which the caller is defined.
    ///   - mdAssemblyRef
    ///     if the type is defined in an assembly other than the one in which the caller is defined.
    ///   - mdTypeRef
    ///     if the type is a nested type.
    ///   - mdModule
    ///     if the type is defined in the same module in which the caller is defined.
    ///   - mdTokenNil
    ///     if the type is defined globally.
    /// - name
    ///   The name of the target type
    pub fn define_type_ref_by_name(
        &self,
        token: mdToken,
        name: &str,
    ) -> Result<mdTypeDef, HRESULT> {
        let wstr = U16CString::from_str(name).unwrap();
        let mut type_ref = mdTypeRefNil;
        let hr = unsafe { self.DefineTypeRefByName(token, wstr.as_ptr(), &mut type_ref) };
        match hr {
            S_OK => Ok(type_ref),
            _ => Err(hr),
        }
    }

    /// Gets a metadata token for the specified literal string.
    pub fn define_user_string(&self, str: &str) -> Result<mdString, HRESULT> {
        let mut md_string = mdStringNil;
        let wstr = U16CString::from_str(str).unwrap();
        let hr =
            unsafe { self.DefineUserString(wstr.as_ptr(), wstr.len() as ULONG, &mut md_string) };
        match hr {
            S_OK => Ok(md_string),
            _ => Err(hr),
        }
    }

    /// Gets a token for the specified metadata signature.
    pub fn get_token_from_sig(&self, sig: &[u8]) -> Result<mdSignature, HRESULT> {
        let mut sig_token = mdSignatureNil;
        let hr = unsafe { self.GetTokenFromSig(sig.as_ptr(), sig.len() as ULONG, &mut sig_token) };
        match hr {
            S_OK => Ok(sig_token),
            _ => Err(hr),
        }
    }

    /// Sets or updates the metadata signature of the inherited method implementation
    /// that is referenced by the specified token.
    pub fn set_method_impl_flags(
        &self,
        method: mdMethodDef,
        implementation: CorMethodImpl,
    ) -> Result<(), HRESULT> {
        let hr = unsafe { self.SetMethodImplFlags(method, implementation.bits()) };
        match hr {
            S_OK => Ok(()),
            _ => Err(hr),
        }
    }
}
