use crate::{ffi::*, types::*};
use com::{
    interfaces::iunknown::IUnknown,
    sys::{FAILED, GUID, HRESULT},
};
use core::ptr;
use std::{ffi::c_void, mem::MaybeUninit};
use widestring::U16CString;

interfaces! {
    #[uuid("7DAC8207-D3AE-4c75-9B67-92801A497D44")]
    pub unsafe interface IMetaDataImport: IUnknown {
        pub unsafe fn CloseEnum(&self, hEnum: HCORENUM);
        pub unsafe fn CountEnum(&self, hEnum: HCORENUM, pulCount: *mut ULONG) -> HRESULT;
        pub unsafe fn ResetEnum(&self, hEnum: HCORENUM, ulPos: *const ULONG) -> HRESULT;
        pub unsafe fn EnumTypeDefs(&self,
            phEnum: *mut HCORENUM,
            rTypeDefs: *const mdTypeDef,
            cMax: ULONG,
            pcTypeDefs: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumInterfaceImpls(&self,
            phEnum: *mut HCORENUM,
            td: mdTypeDef,
            rImpls: *mut mdInterfaceImpl,
            cMax: ULONG,
            pcImpls: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumTypeRefs(&self,
            phEnum: *mut HCORENUM,
            rTypeRefs: *mut mdTypeRef,
            cMax: ULONG,
            pcTypeRefs: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn FindTypeDefByName(&self,
            szTypeDef: LPCWSTR,
            tkEnclosingClass: mdToken,
            ptd: *mut mdTypeDef,
        ) -> HRESULT;
        pub unsafe fn GetScopeProps(&self,
            szName: *mut WCHAR,
            cchName: ULONG,
            pchName: *mut ULONG,
            pmvid: *mut GUID,
        ) -> HRESULT;
        pub unsafe fn GetModuleFromScope(&self, pmd: *mut mdModule) -> HRESULT;
        pub unsafe fn GetTypeDefProps(&self,
            td: mdTypeDef,
            szTypeDef: *mut WCHAR,
            cchTypeDef: ULONG,
            pchTypeDef: *mut ULONG,
            pdwTypeDefFlags: *mut DWORD,
            ptkExtends: *mut mdToken,
        ) -> HRESULT;
        pub unsafe fn GetInterfaceImplProps(&self,
            iiImpl: mdInterfaceImpl,
            pClass: *mut mdTypeDef,
            ptkIface: *mut mdToken,
        ) -> HRESULT;
        pub unsafe fn GetTypeRefProps(&self,
            tr: mdTypeRef,
            ptkResolutionScope: *mut mdToken,
            szName: *mut WCHAR,
            cchName: ULONG,
            pchName: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn ResolveTypeRef(&self,
            tr: mdTypeRef,
            riid: REFIID,
            ppIScope: *mut *mut IUnknown,
            ptd: *mut mdTypeDef,
        ) -> HRESULT;
        pub unsafe fn EnumMembers(&self,
            phEnum: *mut HCORENUM,
            cl: mdTypeDef,
            rMembers: *mut mdToken,
            cMax: ULONG,
            pcTokens: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumMembersWithName(&self,
            phEnum: *mut HCORENUM,
            cl: mdTypeDef,
            szName: LPCWSTR,
            rMembers: *mut mdToken,
            cMax: ULONG,
            pcTokens: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumMethods(&self,
            phEnum: *mut HCORENUM,
            cl: mdTypeDef,
            rMethods: *mut mdMethodDef,
            cMax: ULONG,
            pcTokens: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumMethodsWithName(&self,
            phEnum: *mut HCORENUM,
            cl: mdTypeDef,
            szName: LPCWSTR,
            rMethods: *mut mdMethodDef,
            cMax: ULONG,
            pcTokens: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumFields(&self,
            phEnum: *mut HCORENUM,
            cl: mdTypeDef,
            rFields: *mut mdFieldDef,
            cMax: ULONG,
            pcTokens: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumFieldsWithName(&self,
            phEnum: *mut HCORENUM,
            cl: mdTypeDef,
            szName: LPCWSTR,
            rFields: *mut mdFieldDef,
            cMax: ULONG,
            pcTokens: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumParams(&self,
            phEnum: *mut HCORENUM,
            mb: mdMethodDef,
            rParams: *mut mdParamDef,
            cMax: ULONG,
            pcTokens: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumMemberRefs(&self,
            phEnum: *mut HCORENUM,
            tkParent: mdToken,
            rMemberRefs: *mut mdMemberRef,
            cMax: ULONG,
            pcTokens: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumMethodImpls(&self,
            phEnum: *mut HCORENUM,
            td: mdTypeDef,
            rMethodBody: *mut mdToken,
            rMethodDecl: *mut mdToken,
            cMax: ULONG,
            pcTokens: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumPermissionSets(&self,
            phEnum: *mut HCORENUM,
            tk: mdToken,
            dwActions: DWORD,
            rPermission: *mut mdPermission,
            cMax: ULONG,
            pcTokens: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn FindMember(&self,
            td: mdTypeDef,
            szName: LPCWSTR,
            pvSigBlob: PCCOR_SIGNATURE,
            cbSigBlob: ULONG,
            pmb: *mut mdToken,
        ) -> HRESULT;
        pub unsafe fn FindMethod(&self,
            td: mdTypeDef,
            szName: LPCWSTR,
            pvSigBlob: PCCOR_SIGNATURE,
            cbSigBlob: ULONG,
            pmb: *mut mdMethodDef,
        ) -> HRESULT;
        pub unsafe fn FindField(&self,
            td: mdTypeDef,
            szName: LPCWSTR,
            pvSigBlob: PCCOR_SIGNATURE,
            cbSigBlob: ULONG,
            pmb: *mut mdFieldDef,
        ) -> HRESULT;
        pub unsafe fn FindMemberRef(&self,
            td: mdTypeDef,
            szName: LPCWSTR,
            pvSigBlob: PCCOR_SIGNATURE,
            cbSigBlob: ULONG,
            pmb: *mut mdMemberRef,
        ) -> HRESULT;
        pub unsafe fn GetMethodProps(&self,
            mb: mdMethodDef,
            pClass: *mut mdTypeDef,
            szMethod: *mut WCHAR,
            cchMethod: ULONG,
            pchMethod: *mut ULONG,
            pdwAttr: *mut DWORD,
            ppvSigBlob: *mut PCCOR_SIGNATURE,
            pcbSigBlob: *mut ULONG,
            pulCodeRVA: *mut ULONG,
            pdwImplFlags: *mut DWORD,
        ) -> HRESULT;
        pub unsafe fn GetMemberRefProps(&self,
            mr: mdMemberRef,
            ptk: *mut mdToken,
            szMember: *mut WCHAR,
            cchMember: ULONG,
            pchMember: *mut ULONG,
            ppvSigBlob: *mut PCCOR_SIGNATURE,
            pbSig: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumProperties(&self,
            phEnum: *mut HCORENUM,
            td: mdTypeDef,
            rProperties: *mut mdProperty,
            cMax: ULONG,
            pcProperties: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumEvents(&self,
            phEnum: *mut HCORENUM,
            td: mdTypeDef,
            rEvents: *mut mdEvent,
            cMax: ULONG,
            pcEvents: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetEventProps(&self,
            ev: mdEvent,
            pClass: *mut mdTypeDef,
            szEvent: *mut WCHAR,
            cchEvent: ULONG,
            pchEvent: *mut ULONG,
            pdwEventFlags: *mut DWORD,
            ptkEventType: *mut mdToken,
            pmdAddOn: *mut mdMethodDef,
            pmdRemoveOn: *mut mdMethodDef,
            pmdFire: *mut mdMethodDef,
            rmdOtherMethod: *mut mdMethodDef,
            cMax: ULONG,
            pcOtherMethod: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumMethodSemantics(&self,
            phEnum: *mut HCORENUM,
            mb: mdMethodDef,
            rEventProp: *mut mdToken,
            cMax: ULONG,
            pcEventProp: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetMethodSemantics(&self,
            mb: mdMethodDef,
            tkEventProp: mdToken,
            pdwSemanticsFlags: *mut DWORD,
        ) -> HRESULT;
        pub unsafe fn GetClassLayout(&self,
            td: mdTypeDef,
            pdwPackSize: *mut DWORD,
            rFieldOffset: *mut COR_FIELD_OFFSET,
            cMax: ULONG,
            pcFieldOffset: *mut ULONG,
            pulClassSize: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetFieldMarshal(&self,
            tk: mdToken,
            ppvNativeType: *mut PCCOR_SIGNATURE,
            pcbNativeType: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetRVA(&self,
            tk: mdToken,
            pulCodeRVA: *mut ULONG,
            pdwImplFlags: *mut DWORD,
        ) -> HRESULT;
        pub unsafe fn GetPermissionSetProps(&self,
            pm: mdPermission,
            pdwAction: *mut DWORD,
            ppvPermission: *mut *mut c_void,
            pcbPermission: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetSigFromToken(&self,
            mdSig: mdSignature,
            ppvSig: *mut PCCOR_SIGNATURE,
            pcbSig: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetModuleRefProps(&self,
            mur: mdModuleRef,
            szName: *mut WCHAR,
            cchName: ULONG,
            pchName: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumModuleRefs(&self,
            phEnum: *mut HCORENUM,
            rModuleRefs: *mut mdModuleRef,
            cmax: ULONG,
            pcModuleRefs: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetTypeSpecFromToken(&self,
            typespec: mdTypeSpec,
            ppvSig: *mut PCCOR_SIGNATURE,
            pcbSig: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetNameFromToken(&self,
            tk: mdToken,
            pszUtf8NamePtr: *mut MDUTF8CSTR,
        ) -> HRESULT;
        pub unsafe fn EnumUnresolvedMethods(&self,
            phEnum: *mut HCORENUM,
            rMethods: *mut mdToken,
            cMax: ULONG,
            pcTokens: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetUserString(&self,
            stk: mdString,
            szString: *mut WCHAR,
            cchString: ULONG,
            pchString: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetPinvokeMap(&self,
            tk: mdToken,
            pdwMappingFlags: *mut DWORD,
            szImportName: *mut WCHAR,
            cchImportName: ULONG,
            pchImportName: *mut ULONG,
            pmrImportDLL: *mut mdModuleRef,
        ) -> HRESULT;
        pub unsafe fn EnumSignatures(&self,
            phEnum: *mut HCORENUM,
            rSignatures: *mut mdSignature,
            cMax: ULONG,
            pcSignatures: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumTypeSpecs(&self,
            phEnum: *mut HCORENUM,
            rTypeSpecs: *mut mdTypeSpec,
            cMax: ULONG,
            pcTypeSpecs: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumUserStrings(&self,
            phEnum: *mut HCORENUM,
            rStrings: *mut mdString,
            cMax: ULONG,
            pcStrings: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetParamForMethodIndex(&self,
            md: mdMethodDef,
            ulParamSeq: ULONG,
            ppd: *mut mdParamDef,
        ) -> HRESULT;
        pub unsafe fn EnumCustomAttributes(&self,
            phEnum: *mut HCORENUM,
            tk: mdToken,
            tkType: mdToken,
            rCustomAttributes: *mut mdCustomAttribute,
            cMax: ULONG,
            pcCustomAttributes: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetCustomAttributeProps(&self,
            cv: mdCustomAttribute,
            ptkObj: *mut mdToken,
            ptkType: *mut mdToken,
            ppBlob: *mut *mut c_void,
            pcbSize: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn FindTypeRef(&self,
            tkResolutionScope: mdToken,
            szName: LPCWSTR,
            ptr: *mut mdTypeRef,
        ) -> HRESULT;
        pub unsafe fn GetMemberProps(&self,
            mb: mdToken,
            pClass: *mut mdTypeDef,
            szMember: *mut WCHAR,
            cchMember: ULONG,
            pchMember: *mut ULONG,
            pdwAttr: *mut DWORD,
            ppvSigBlob: *mut PCCOR_SIGNATURE,
            pcbSigBlob: *mut ULONG,
            pulCodeRVA: *mut ULONG,
            pdwImplFlags: *mut DWORD,
            pdwCPlusTypeFlag: *mut DWORD,
            ppValue: *mut UVCP_CONSTANT,
            pcchValue: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetFieldProps(&self,
            mb: mdToken,
            pClass: *mut mdTypeDef,
            szField: *mut WCHAR,
            cchField: ULONG,
            pchField: *mut ULONG,
            pdwAttr: *mut DWORD,
            ppvSigBlob: *mut PCCOR_SIGNATURE,
            pcbSigBlob: *mut ULONG,
            pdwCPlusTypeFlag: *mut DWORD,
            ppValue: *mut UVCP_CONSTANT,
            pcchValue: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetPropertyProps(&self,
            prop: mdProperty,
            pClass: *mut mdTypeDef,
            szProperty: *mut WCHAR,
            cchProperty: ULONG,
            pchProperty: *mut ULONG,
            pdwPropFlags: *mut DWORD,
            ppvSig: *mut PCCOR_SIGNATURE,
            pbSig: *mut ULONG,
            pdwCPlusTypeFlag: *mut DWORD,
            ppDefaultValue: *mut UVCP_CONSTANT,
            pcchDefaultValue: *mut ULONG,
            pmdSetter: *mut mdMethodDef,
            pmdGetter: *mut mdMethodDef,
            rmdOtherMethod: *mut mdMethodDef,
            cMax: ULONG,
            pcOtherMethod: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetParamProps(&self,
            tk: mdParamDef,
            pmd: *mut mdMethodDef,
            pulSequence: *mut ULONG,
            szName: *mut WCHAR,
            cchName: ULONG,
            pchName: *mut ULONG,
            pdwAttr: *mut DWORD,
            pdwCPlusTypeFlag: *mut DWORD,
            ppValue: *mut UVCP_CONSTANT,
            pcchValue: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetCustomAttributeByName(&self,
            tkObj: mdToken,
            szName: LPCWSTR,
            ppData: *mut *mut c_void,
            pcbData: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn IsValidToken(&self, tk: mdToken) -> BOOL;
        pub unsafe fn GetNestedClassProps(&self,
            tdNestedClass: mdTypeDef,
            ptdEnclosingClass: *mut mdTypeDef,
        ) -> HRESULT;
        pub unsafe fn GetNativeCallConvFromSig(&self,
            pvSig: *const c_void,
            cbSig: ULONG,
            pCallConv: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn IsGlobal(&self, pd: mdToken, pbGlobal: *mut int) -> HRESULT;
    }

    #[uuid("FCE5EFA0-8BBA-4F8E-A036-8F2022B08466")]
    pub unsafe interface IMetaDataImport2: IMetaDataImport {
        pub unsafe fn EnumGenericParams(&self,
            phEnum: *mut HCORENUM,
            tk: mdToken,
            rGenericParams: *mut mdGenericParam,
            cMax: ULONG,
            pcGenericParams: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetGenericParamProps(&self,
            gp: mdGenericParam,
            pulParamSeq: *mut ULONG,
            pdwParamFlags: *mut DWORD,
            ptOwner: *mut mdToken,
            reserved: *mut DWORD,
            wzname: *mut WCHAR,
            cchName: ULONG,
            pchName: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetMethodSpecProps(&self,
            mi: mdMethodSpec,
            tkParent: *mut mdToken,
            ppvSigBlob: *mut PCCOR_SIGNATURE,
            pcbSigBlob: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn EnumGenericParamConstraints(&self,
            phEnum: *mut HCORENUM,
            tk: mdGenericParam,
            rGenericParamConstraints: *mut mdGenericParamConstraint,
            cMax: ULONG,
            pcGenericParamConstraints: *mut ULONG,
        ) -> HRESULT;
        pub unsafe fn GetGenericParamConstraintProps(&self,
            gpc: mdGenericParamConstraint,
            ptGenericParam: *mut mdGenericParam,
            ptkConstraintType: *mut mdToken,
        ) -> HRESULT;
        pub unsafe fn GetPEKind(&self,
            pdwPEKind: *mut DWORD,
            pdwMAchine: *mut DWORD,
        ) -> HRESULT;
        pub unsafe fn GetVersionString(&self,
            pwzBuf: *mut WCHAR,
            ccBufSize: DWORD,
            pccBufSize: *mut DWORD,
        ) -> HRESULT;
        pub unsafe fn EnumMethodSpecs(&self,
            phEnum: *mut HCORENUM,
            tk: mdToken,
            rMethodSpecs: *mut mdMethodSpec,
            cMax: ULONG,
            pcMethodSpecs: *mut ULONG,
        ) -> HRESULT;
    }
}

impl IMetaDataImport {
    pub fn find_type_def_by_name(
        &self,
        name: &str,
        enclosing_class: Option<mdToken>,
    ) -> Result<mdTypeDef, HRESULT> {
        let wide_name = U16CString::from_str(name).unwrap();
        let mut type_def = MaybeUninit::uninit();
        let hr = unsafe {
            match enclosing_class {
                Some(t) => self.FindTypeDefByName(wide_name.as_ptr(), t, type_def.as_mut_ptr()),
                None => self.FindTypeDefByName(wide_name.as_ptr(), 0, type_def.as_mut_ptr()),
            }
        };
        match hr {
            S_OK => {
                let type_def = unsafe { type_def.assume_init() };
                Ok(type_def)
            }
            _ => Err(hr),
        }
    }

    pub fn enum_methods_with_name(
        &self,
        type_def: mdTypeDef,
        name: &str,
    ) -> Result<Vec<mdMethodDef>, HRESULT> {
        let mut en = ptr::null_mut() as HCORENUM;
        let wide_name = U16CString::from_str(name).unwrap();
        let max = 256;
        let mut method_defs = Vec::with_capacity(max as usize);
        let mut method_len = MaybeUninit::uninit();
        let hr = unsafe {
            self.EnumMethodsWithName(
                &mut en,
                type_def,
                wide_name.as_ptr(),
                method_defs.as_mut_ptr(),
                max,
                method_len.as_mut_ptr(),
            )
        };

        match hr {
            S_OK => {
                unsafe {
                    let len = method_len.assume_init();
                    method_defs.set_len(len as usize);
                    self.CloseEnum(en);
                }
                Ok(method_defs)
            }
            S_FALSE => Ok(Vec::new()),
            _ => Err(hr),
        }
    }

    /// Gets metadata associated with the member referenced by the specified token.
    pub fn get_member_ref_props(&self, mr: mdMemberRef) -> Result<MemberRefProps, HRESULT> {
        let mut name_buffer_length = MaybeUninit::uninit();
        let hr = unsafe {
            self.GetMemberRefProps(
                mr,
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
        let mut class_token = MaybeUninit::uninit();
        let mut pb_sig_blob = MaybeUninit::uninit();
        let mut pb_sig = MaybeUninit::uninit();

        let hr = unsafe {
            self.GetMemberRefProps(
                mr,
                class_token.as_mut_ptr(),
                name_buffer.as_mut_ptr(),
                name_buffer_length,
                name_length.as_mut_ptr(),
                pb_sig_blob.as_mut_ptr(),
                pb_sig.as_mut_ptr(),
            )
        };

        if FAILED(hr) {
            return Err(hr);
        }

        let class_token = unsafe { class_token.assume_init() };
        let name = U16CString::from_vec_with_nul(name_buffer)
            .unwrap()
            .to_string_lossy();
        let signature = unsafe {
            let pb_sig_blob = pb_sig_blob.assume_init();
            let pb_sig = pb_sig.assume_init();
            std::slice::from_raw_parts(pb_sig_blob, pb_sig as usize).to_vec()
        };

        Ok(MemberRefProps {
            name,
            class_token,
            signature,
        })
    }

    /// Gets information stored in the metadata for a specified member definition,
    /// including the name, binary signature, and relative virtual address, of the Type member
    /// referenced by the specified metadata token. This is a simple helper method: if mb is a
    /// MethodDef, then GetMethodProps is called; if mb is a FieldDef, then GetFieldProps is
    /// called. See these other methods for details.
    pub fn get_member_props(&self, mb: mdToken) -> Result<MemberProps, HRESULT> {
        let mut name_buffer_length = MaybeUninit::uninit();
        let hr = unsafe {
            self.GetMemberProps(
                mb,
                ptr::null_mut(),
                ptr::null_mut(),
                0,
                name_buffer_length.as_mut_ptr(),
                ptr::null_mut(),
                ptr::null_mut(),
                ptr::null_mut(),
                ptr::null_mut(),
                ptr::null_mut(),
                ptr::null_mut(),
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
        let mut class_token = MaybeUninit::uninit();
        let mut pdw_attr = MaybeUninit::uninit();
        let mut impl_flags = MaybeUninit::uninit();
        let mut element_type = MaybeUninit::uninit();
        let mut ppv_sig_blob = MaybeUninit::uninit();
        let mut pcb_sig_blob = MaybeUninit::uninit();
        let mut pul_code_rva = MaybeUninit::uninit();
        let mut value = MaybeUninit::uninit();
        let mut value_len = MaybeUninit::uninit();

        let hr = unsafe {
            self.GetMemberProps(
                mb,
                class_token.as_mut_ptr(),
                name_buffer.as_mut_ptr(),
                name_buffer_length,
                name_length.as_mut_ptr(),
                pdw_attr.as_mut_ptr(),
                ppv_sig_blob.as_mut_ptr(),
                pcb_sig_blob.as_mut_ptr(),
                pul_code_rva.as_mut_ptr(),
                impl_flags.as_mut_ptr(),
                element_type.as_mut_ptr(),
                value.as_mut_ptr(),
                value_len.as_mut_ptr(),
            )
        };

        if FAILED(hr) {
            return Err(hr);
        }

        let name = U16CString::from_vec_with_nul(name_buffer)
            .unwrap()
            .to_string_lossy();
        let class_token = unsafe { class_token.assume_init() };
        let pdw_attr = unsafe { pdw_attr.assume_init() };
        let impl_flags = unsafe { impl_flags.assume_init() };
        let element_type = unsafe { element_type.assume_init() };
        let signature = unsafe {
            let ppv_sig_blob = ppv_sig_blob.assume_init();
            let pcb_sig_blob = pcb_sig_blob.assume_init();
            std::slice::from_raw_parts(ppv_sig_blob, pcb_sig_blob as usize).to_vec()
        };
        let pul_code_rva = unsafe { pul_code_rva.assume_init() };
        let value_len = unsafe { value_len.assume_init() };
        let value = if value_len > 0 {
            unsafe {
                let _value = value.assume_init();
                //slice_from_raw_parts(value, value_len as usize)
                // TODO: get string from value
                String::new()
            }
        } else {
            String::new()
        };

        Ok(MemberProps {
            name,
            class_token,
            member_flags: pdw_attr,
            relative_virtual_address: pul_code_rva,
            method_impl_flags: impl_flags,
            element_type: CorElementType::from(element_type),
            signature,
            value,
        })
    }

    /// Gets the metadata associated with the method referenced by the specified MethodDef token.
    pub fn get_method_props(&self, mb: mdMethodDef) -> Result<MethodProps, HRESULT> {
        let mut name_buffer_length = MaybeUninit::uninit();
        unsafe {
            self.GetMethodProps(
                mb,
                ptr::null_mut(),
                ptr::null_mut(),
                0,
                name_buffer_length.as_mut_ptr(),
                ptr::null_mut(),
                ptr::null_mut(),
                ptr::null_mut(),
                ptr::null_mut(),
                ptr::null_mut(),
            )
        };

        let mut class_token = MaybeUninit::uninit();
        let name_buffer_length = unsafe { name_buffer_length.assume_init() };
        let mut name_buffer = Vec::<WCHAR>::with_capacity(name_buffer_length as usize);
        unsafe { name_buffer.set_len(name_buffer_length as usize) };
        let mut name_length = MaybeUninit::uninit();
        let mut attr_flags = MaybeUninit::uninit();
        let mut sig = MaybeUninit::uninit();
        let mut sig_length = MaybeUninit::uninit();
        let mut rva = MaybeUninit::uninit();
        let mut impl_flags = MaybeUninit::uninit();
        let hr = unsafe {
            self.GetMethodProps(
                mb,
                class_token.as_mut_ptr(),
                name_buffer.as_mut_ptr(),
                name_buffer_length,
                name_length.as_mut_ptr(),
                attr_flags.as_mut_ptr(),
                sig.as_mut_ptr(),
                sig_length.as_mut_ptr(),
                rva.as_mut_ptr(),
                impl_flags.as_mut_ptr(),
            )
        };
        match hr {
            S_OK => {
                let class_token = unsafe { class_token.assume_init() };
                let name = U16CString::from_vec_with_nul(name_buffer)
                    .unwrap()
                    .to_string_lossy();
                let attr_flags = unsafe { attr_flags.assume_init() };
                let attr_flags = CorMethodAttr::from_bits(attr_flags).unwrap();
                let sig = unsafe { sig.assume_init() };
                let sig_length = unsafe { sig_length.assume_init() };
                let rva = unsafe { rva.assume_init() };
                let impl_flags = unsafe { impl_flags.assume_init() };
                let impl_flags = CorMethodImpl::from_bits(impl_flags).unwrap();
                Ok(MethodProps {
                    class_token,
                    name,
                    attr_flags,
                    sig,
                    sig_length,
                    rva,
                    impl_flags,
                })
            }
            _ => Err(hr),
        }
    }

    pub fn get_type_def_props(&self, td: mdTypeDef) -> Result<TypeDefProps, HRESULT> {
        let mut name_buffer_length = MaybeUninit::uninit();
        let hr = unsafe {
            self.GetTypeDefProps(
                td,
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
        let mut cor_type_attr = MaybeUninit::uninit();
        let mut extends_td = MaybeUninit::uninit();

        let hr = unsafe {
            self.GetTypeDefProps(
                td,
                name_buffer.as_mut_ptr(),
                name_buffer_length,
                name_length.as_mut_ptr(),
                cor_type_attr.as_mut_ptr(),
                extends_td.as_mut_ptr(),
            )
        };

        if FAILED(hr) {
            return Err(hr);
        }

        let name = U16CString::from_vec_with_nul(name_buffer)
            .unwrap()
            .to_string_lossy();
        let cor_type_attr = {
            let c = unsafe { cor_type_attr.assume_init() };
            CorTypeAttr::from_bits(c).unwrap()
        };
        let extends_td = unsafe { extends_td.assume_init() };
        Ok(TypeDefProps {
            name,
            cor_type_attr,
            extends_td,
        })
    }

    pub fn get_user_string(&self, stk: mdString) -> Result<String, HRESULT> {
        let mut len = MaybeUninit::uninit();
        let hr = unsafe { self.GetUserString(stk, ptr::null_mut(), 0, len.as_mut_ptr()) };

        if FAILED(hr) {
            return Err(hr);
        }

        let len = unsafe { len.assume_init() };
        let mut str_buffer = Vec::<WCHAR>::with_capacity(len as usize);
        unsafe { str_buffer.set_len(len as usize) };
        let hr = unsafe { self.GetUserString(stk, str_buffer.as_mut_ptr(), len, ptr::null_mut()) };

        if FAILED(hr) {
            return Err(hr);
        }

        // NOTE: the user string is not null terminated
        let str = String::from_utf16(&str_buffer).unwrap();
        Ok(str)
    }
}

impl IMetaDataImport2 {
    /// Gets the metadata signature of the method referenced by the specified MethodSpec token.
    pub fn get_method_spec_props(&self, mi: mdMethodSpec) -> Result<MethodSpecProps, HRESULT> {
        let mut tk_parent = MaybeUninit::uninit();
        let mut ppv_sig_blob = MaybeUninit::uninit();
        let mut pcb_sig_blob = MaybeUninit::uninit();
        let hr = unsafe {
            self.GetMethodSpecProps(
                mi,
                tk_parent.as_mut_ptr(),
                ppv_sig_blob.as_mut_ptr(),
                pcb_sig_blob.as_mut_ptr(),
            )
        };

        if FAILED(hr) {
            return Err(hr);
        }

        let parent = unsafe { tk_parent.assume_init() };
        let signature = unsafe {
            let pcb_sig_blob = pcb_sig_blob.assume_init();
            let ppv_sig_blob = ppv_sig_blob.assume_init();
            std::slice::from_raw_parts(ppv_sig_blob, pcb_sig_blob as usize).to_vec()
        };

        Ok(MethodSpecProps { parent, signature })
    }

    // other methods, not direct Rust abstractions over COM

    pub fn get_function_info(&self, token: mdToken) -> Result<MyFunctionInfo, HRESULT> {
        let cor_token_type = {
            let t = type_from_token(token);
            CorTokenType::from_bits(t).unwrap()
        };

        let mut is_generic = false;
        match cor_token_type {
            CorTokenType::mdtMemberRef => {
                let member_ref_props = self.get_member_ref_props(token)?;
                Ok(MyFunctionInfo {
                    name: member_ref_props.name,
                    is_generic,
                    signature: member_ref_props.signature,
                })
            }
            CorTokenType::mdtMethodDef => {
                let member_props = self.get_member_props(token)?;
                Ok(MyFunctionInfo {
                    name: member_props.name,
                    is_generic,
                    signature: member_props.signature,
                })
            }
            CorTokenType::mdtMethodSpec => {
                let method_spec = self.get_method_spec_props(token)?;
                is_generic = true;
                let generic_info = self.get_function_info(method_spec.parent)?;
                Ok(MyFunctionInfo {
                    name: generic_info.name,
                    is_generic,
                    signature: method_spec.signature,
                })
            }
            _ => {
                log::warn!("Unknown token type {}", token);
                Err(E_FAIL)
            }
        }
    }
}
