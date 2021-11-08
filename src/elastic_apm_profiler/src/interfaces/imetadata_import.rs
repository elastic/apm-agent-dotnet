// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use core::{ptr, slice};
use std::{ffi::c_void, mem::MaybeUninit};

use com::{
    interfaces::iunknown::IUnknown,
    sys::{FAILED, GUID, HRESULT, S_FALSE, S_OK},
};
use widestring::U16CString;

use crate::{
    cil::uncompress_token,
    ffi::{types::*, *},
    profiler::types::{FunctionInfo, FunctionMethodSignature, MethodSignature, TypeInfo},
};

interfaces! {
    /// Provides methods for importing and manipulating existing metadata from a portable
    /// executable (PE) file or other source, such as a type library or a stand-alone,
    /// run-time metadata binary.
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
            td: mdTypeRef,
            szName: LPCWSTR,
            pvSigBlob: PCCOR_SIGNATURE,
            cbSigBlob: ULONG,
            pmr: *mut mdMemberRef,
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

    /// Extends the IMetaDataImport interface to provide the capability of working
    /// with generic types.
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
    pub fn find_member_ref(
        &self,
        type_ref: mdTypeRef,
        name: &str,
        signature: &[u8],
    ) -> Result<mdMemberRef, HRESULT> {
        let wide_name = U16CString::from_str(name).unwrap();
        let mut member_ref = mdMemberRefNil;
        let hr = unsafe {
            self.FindMemberRef(
                type_ref,
                wide_name.as_ptr(),
                signature.as_ptr(),
                signature.len() as ULONG,
                &mut member_ref,
            )
        };
        match hr {
            S_OK => Ok(member_ref),
            _ => Err(hr),
        }
    }

    /// Gets a pointer to the TypeDef metadata token for the Type with the specified name.
    pub fn find_type_def_by_name(
        &self,
        name: &str,
        enclosing_class: Option<mdToken>,
    ) -> Result<mdTypeDef, HRESULT> {
        let wide_name = U16CString::from_str(name).unwrap();
        let mut type_def = mdTypeDefNil;
        let hr = unsafe {
            self.FindTypeDefByName(
                wide_name.as_ptr(),
                enclosing_class.unwrap_or(mdTokenNil),
                &mut type_def,
            )
        };
        match hr {
            S_OK => Ok(type_def),
            _ => Err(hr),
        }
    }

    pub fn find_type_ref(&self, scope: mdToken, name: &str) -> Result<mdTypeRef, HRESULT> {
        let wide_name = U16CString::from_str(name).unwrap();
        let mut type_ref = mdTypeRefNil;

        let hr = unsafe { self.FindTypeRef(scope, wide_name.as_ptr(), &mut type_ref) };

        match hr {
            S_OK => Ok(type_ref),
            _ => Err(hr),
        }
    }

    /// Enumerates methods that have the specified name and that are defined by the
    /// type referenced by the specified TypeDef token.
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
                // TODO: get string from value
                //slice_from_raw_parts(value, value_len as usize)
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

    pub fn get_module_from_scope(&self) -> Result<mdModule, HRESULT> {
        let mut module = mdModuleNil;
        let hr = unsafe { self.GetModuleFromScope(&mut module) };
        match hr {
            S_OK => Ok(module),
            _ => Err(hr),
        }
    }

    /// Gets the name of the module referenced by the specified metadata token.
    pub fn get_module_ref_props(&self, token: mdModuleRef) -> Result<ModuleRefProps, HRESULT> {
        let mut name_buffer_length = MaybeUninit::uninit();
        let hr = unsafe {
            self.GetModuleRefProps(token, ptr::null_mut(), 0, name_buffer_length.as_mut_ptr())
        };

        if FAILED(hr) {
            return Err(hr);
        }

        let name_buffer_length = unsafe { name_buffer_length.assume_init() };
        let mut name_buffer = Vec::<WCHAR>::with_capacity(name_buffer_length as usize);
        unsafe { name_buffer.set_len(name_buffer_length as usize) };
        let mut name_length = MaybeUninit::uninit();

        let hr = unsafe {
            self.GetModuleRefProps(
                token,
                name_buffer.as_mut_ptr(),
                name_buffer_length,
                name_length.as_mut_ptr(),
            )
        };

        if FAILED(hr) {
            return Err(hr);
        }

        let name = U16CString::from_vec_with_nul(name_buffer)
            .unwrap()
            .to_string_lossy();

        Ok(ModuleRefProps { name })
    }

    pub fn get_nested_class_props(&self, token: mdTypeDef) -> Result<mdTypeDef, HRESULT> {
        let mut parent_token = mdTypeDefNil;
        let hr = unsafe { self.GetNestedClassProps(token, &mut parent_token) };
        match hr {
            S_OK => Ok(parent_token),
            _ => Err(hr),
        }
    }

    pub fn get_sig_from_token(&self, token: mdSignature) -> Result<Vec<u8>, HRESULT> {
        let mut sig = MaybeUninit::uninit();
        let mut len = 0;
        let hr = unsafe { self.GetSigFromToken(token, sig.as_mut_ptr(), &mut len) };
        match hr {
            S_OK => {
                let signature = unsafe {
                    let s = sig.assume_init();
                    slice::from_raw_parts(s as *const u8, len as usize).to_vec()
                };
                Ok(signature)
            }
            _ => Err(hr),
        }
    }

    /// Gets metadata information for the Type represented by the specified metadata token.
    pub fn get_type_def_props(&self, token: mdTypeDef) -> Result<TypeDefProps, HRESULT> {
        let mut name_buffer_length = MaybeUninit::uninit();
        let hr = unsafe {
            self.GetTypeDefProps(
                token,
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
                token,
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

    /// Gets the metadata associated with the Type referenced by the specified TypeRef token.
    pub fn get_type_ref_props(&self, token: mdTypeRef) -> Result<TypeRefProps, HRESULT> {
        let mut name_buffer_length = MaybeUninit::uninit();
        let mut parent_token = mdTokenNil;

        let hr = unsafe {
            self.GetTypeRefProps(
                token,
                &mut parent_token,
                ptr::null_mut(),
                0,
                name_buffer_length.as_mut_ptr(),
            )
        };

        if FAILED(hr) {
            return Err(hr);
        }

        let name_buffer_length = unsafe { name_buffer_length.assume_init() };
        let mut name_buffer = Vec::<WCHAR>::with_capacity(name_buffer_length as usize);
        unsafe { name_buffer.set_len(name_buffer_length as usize) };
        let mut name_length = MaybeUninit::uninit();

        let hr = unsafe {
            self.GetTypeRefProps(
                token,
                &mut parent_token,
                name_buffer.as_mut_ptr(),
                name_buffer_length,
                name_length.as_mut_ptr(),
            )
        };

        if FAILED(hr) {
            return Err(hr);
        }

        let name = U16CString::from_vec_with_nul(name_buffer)
            .unwrap()
            .to_string_lossy();

        Ok(TypeRefProps { name, parent_token })
    }

    /// Gets the binary metadata signature of the type specification represented by
    /// the specified token.
    pub fn get_type_spec_from_token(&self, token: mdTypeSpec) -> Result<TypeSpec, HRESULT> {
        let mut signature = MaybeUninit::uninit();
        let mut signature_len = MaybeUninit::uninit();
        let hr = unsafe {
            self.GetTypeSpecFromToken(token, signature.as_mut_ptr(), signature_len.as_mut_ptr())
        };

        if FAILED(hr) {
            return Err(hr);
        }

        let signature = unsafe {
            let s = signature.assume_init();
            let l = signature_len.assume_init();
            std::slice::from_raw_parts(s, l as usize).to_vec()
        };

        Ok(TypeSpec { signature })
    }

    /// Gets the literal string represented by the specified metadata token.
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
    pub fn get_method_spec_props(&self, token: mdMethodSpec) -> Result<MethodSpecProps, HRESULT> {
        let mut parent = MaybeUninit::uninit();
        let mut signature = MaybeUninit::uninit();
        let mut signature_len = MaybeUninit::uninit();
        let hr = unsafe {
            self.GetMethodSpecProps(
                token,
                parent.as_mut_ptr(),
                signature.as_mut_ptr(),
                signature_len.as_mut_ptr(),
            )
        };

        if FAILED(hr) {
            return Err(hr);
        }

        let parent = unsafe { parent.assume_init() };
        let signature = unsafe {
            let s = signature.assume_init();
            let l = signature_len.assume_init();
            std::slice::from_raw_parts(s, l as usize).to_vec()
        };

        Ok(MethodSpecProps { parent, signature })
    }

    // other methods, not direct Rust abstractions over COM

    /// Gets the function information from the specified metadata token
    pub fn get_function_info(&self, token: mdToken) -> Result<FunctionInfo, HRESULT> {
        let cor_token_type = {
            let t = type_from_token(token);
            CorTokenType::from_bits(t).unwrap()
        };

        let mut is_generic = false;
        let function_name;
        let parent_token;
        let mut method_def_id = mdTokenNil;
        let final_signature;
        let method_signature;
        let mut method_spec_signature = None;

        match cor_token_type {
            CorTokenType::mdtMemberRef => {
                let member_ref_props = self.get_member_ref_props(token)?;
                function_name = member_ref_props.name;
                parent_token = member_ref_props.class_token;
                final_signature = MethodSignature::new(member_ref_props.signature.clone());
                method_signature = FunctionMethodSignature::new(member_ref_props.signature);
            }
            CorTokenType::mdtMethodDef => {
                let member_props = self.get_member_props(token)?;
                function_name = member_props.name;
                parent_token = member_props.class_token;
                final_signature = MethodSignature::new(member_props.signature.clone());
                method_signature = FunctionMethodSignature::new(member_props.signature);
            }
            CorTokenType::mdtMethodSpec => {
                let method_spec = self.get_method_spec_props(token)?;
                parent_token = method_spec.parent;

                is_generic = true;
                let generic_info = self.get_function_info(parent_token)?;

                function_name = generic_info.name;
                final_signature = generic_info.signature;
                method_signature = FunctionMethodSignature::new(method_spec.signature.clone());
                method_spec_signature = Some(MethodSignature::new(method_spec.signature));
                method_def_id = generic_info.id;
            }
            _ => {
                log::warn!("get_function_info: unknown token type {}", token);
                return Err(E_FAIL);
            }
        };

        let type_info = self.get_type_info(parent_token)?;

        Ok(FunctionInfo::new(
            token,
            function_name,
            is_generic,
            type_info,
            final_signature,
            method_spec_signature,
            method_def_id,
            method_signature,
        ))
    }

    /// Gets the type information for the specified metadata token
    pub fn get_type_info(&self, token: mdToken) -> Result<Option<TypeInfo>, HRESULT> {
        let token_type = {
            let t = type_from_token(token);
            CorTokenType::from_bits(t).unwrap()
        };

        let mut parent_type = None;
        let mut extends_from = None;
        let mut name: String = String::new();
        let mut is_value_type = false;
        let mut is_generic = false;

        match token_type {
            CorTokenType::mdtTypeDef => {
                let type_def_props = self.get_type_def_props(token)?;
                name = type_def_props.name;

                let mut parent_type_token = mdTokenNil;

                // try to get the parent type if type is nested
                let hr = unsafe { self.GetNestedClassProps(token, &mut parent_type_token) };
                if parent_type_token != mdTokenNil {
                    parent_type = Some(Box::new(self.get_type_info(parent_type_token)?.unwrap()));
                }

                // get the base type
                if type_def_props.extends_td != mdTokenNil {
                    if let Some(extends_type_info) =
                        self.get_type_info(type_def_props.extends_td)?
                    {
                        is_value_type = &extends_type_info.name == "System.ValueType"
                            || &extends_type_info.name == "System.Enum";
                        extends_from = Some(Box::new(extends_type_info));
                    }
                }
            }
            CorTokenType::mdtTypeRef => {
                let type_ref_props = self.get_type_ref_props(token)?;
                name = type_ref_props.name;
            }
            CorTokenType::mdtTypeSpec => {
                let type_spec = self.get_type_spec_from_token(token)?;

                if type_spec.signature.len() < 3 {
                    return Ok(None);
                }

                if type_spec.signature[0] == CorElementType::ELEMENT_TYPE_GENERICINST as u8 {
                    let (base_token, base_len) = uncompress_token(&type_spec.signature[2..]);
                    return if let Some(base_type) = self.get_type_info(base_token)? {
                        Ok(Some(TypeInfo {
                            id: base_type.id,
                            name: base_type.name,
                            type_spec: token,
                            token_type: base_type.token_type,
                            extends_from: base_type.extends_from,
                            is_value_type: base_type.is_value_type,
                            is_generic: base_type.is_generic,
                            parent_type: base_type.parent_type,
                        }))
                    } else {
                        Ok(None)
                    };
                }
            }
            CorTokenType::mdtModuleRef => {
                let module_ref_props = self.get_module_ref_props(token)?;
                name = module_ref_props.name;
            }
            CorTokenType::mdtMemberRef => {
                let function_info = self.get_function_info(token)?;
                return Ok(function_info.type_info);
            }
            CorTokenType::mdtMethodDef => {
                let function_info = self.get_function_info(token)?;
                return Ok(function_info.type_info);
            }
            _ => return Ok(None),
        };

        // check the type name for generic arity
        if let Some(index) = name.rfind('`') {
            let from_right = name.len() - index - 1;
            is_generic = from_right == 1 || from_right == 2;
        }

        Ok(Some(TypeInfo {
            id: token,
            name,
            type_spec: mdTypeSpecNil,
            token_type,
            extends_from,
            is_value_type,
            is_generic,
            parent_type,
        }))
    }

    pub fn get_module_version_id(&self) -> Result<GUID, HRESULT> {
        let mut module = mdModuleNil;
        let hr = unsafe { self.GetModuleFromScope(&mut module) };

        if FAILED(hr) {
            return Err(hr);
        }

        let mut module_version_id = MaybeUninit::uninit();

        let hr = unsafe {
            self.GetScopeProps(
                ptr::null_mut(),
                0,
                ptr::null_mut(),
                module_version_id.as_mut_ptr(),
            )
        };

        if FAILED(hr) {
            return Err(hr);
        }

        let module_version_id = unsafe { module_version_id.assume_init() };
        Ok(module_version_id)
    }
}
