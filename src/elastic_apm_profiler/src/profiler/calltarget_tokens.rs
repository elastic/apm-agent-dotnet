// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use std::ops::Deref;

use com::sys::HRESULT;
use widestring::U16CString;

use crate::{
    cil::{compress_data, compress_token, uncompress_data, Instruction, Method},
    ffi::{
        mdAssemblyRef, mdAssemblyRefNil, mdMemberRef, mdMemberRefNil, mdMethodSpec,
        mdMethodSpecNil, mdToken, mdTokenNil, mdTypeRef, mdTypeRefNil, mdTypeSpec, mdTypeSpecNil,
        CorAssemblyFlags, CorCallingConvention, CorElementType,
        CorElementType::ELEMENT_TYPE_VALUETYPE, ASSEMBLYMETADATA,
        COR_PRF_SNAPSHOT_INFO::COR_PRF_SNAPSHOT_REGISTER_CONTEXT,
        COR_PRF_TRANSITION_REASON::COR_PRF_TRANSITION_CALL, COR_SIGNATURE, E_FAIL, ULONG, WCHAR,
    },
    profiler::{
        managed,
        types::{
            FunctionMethodArgument, MetadataBuilder, MethodArgumentTypeFlag, ModuleMetadata,
            MyFunctionInfo, MyTypeInfo,
        },
    },
};

/// Metadata tokens to modify call targets
pub struct CallTargetTokens {
    cor_lib_assembly_ref: mdAssemblyRef,
    object_type_ref: mdTypeRef,
    ex_type_ref: mdTypeRef,
    type_ref: mdTypeRef,
    runtime_type_handle_ref: mdTypeRef,
    get_type_from_handle_token: mdToken,
    runtime_method_handle_ref: mdTypeRef,
    profiler_assembly_ref: mdAssemblyRef,
    call_target_type_ref: mdTypeRef,
    call_target_state_type_ref: mdTypeRef,
    call_target_return_void_type_ref: mdTypeRef,
    call_target_return_type_ref: mdTypeRef,
    begin_array_member_ref: mdMemberRef,
    begin_method_fast_path_refs: Vec<mdMemberRef>,
    end_void_member_ref: mdMemberRef,
    log_exception_ref: mdMemberRef,
    call_target_state_type_get_default: mdMemberRef,
    call_target_return_void_type_get_default: mdMemberRef,
    get_default_member_ref: mdMemberRef,
}

impl CallTargetTokens {
    pub const FAST_PATH_COUNT: usize = 9;
    pub fn new() -> Self {
        Self {
            cor_lib_assembly_ref: mdAssemblyRefNil,
            object_type_ref: mdTypeRefNil,
            ex_type_ref: mdTypeRefNil,
            type_ref: mdTypeRefNil,
            runtime_type_handle_ref: mdTypeRefNil,
            get_type_from_handle_token: mdTokenNil,
            runtime_method_handle_ref: mdTypeRefNil,
            profiler_assembly_ref: mdAssemblyRefNil,
            call_target_type_ref: mdTypeRefNil,
            call_target_state_type_ref: mdTypeRefNil,
            call_target_return_void_type_ref: mdTypeRefNil,
            call_target_return_type_ref: mdTypeRefNil,
            begin_array_member_ref: mdMemberRefNil,
            begin_method_fast_path_refs: vec![mdMemberRefNil; Self::FAST_PATH_COUNT],
            end_void_member_ref: mdMemberRefNil,
            log_exception_ref: mdMemberRefNil,
            call_target_state_type_get_default: mdMemberRefNil,
            call_target_return_void_type_get_default: mdMemberRefNil,
            get_default_member_ref: mdMemberRefNil,
        }
    }

    pub fn ensure_cor_lib_tokens(
        &mut self,
        module_metadata: &ModuleMetadata,
    ) -> Result<(), HRESULT> {
        if self.cor_lib_assembly_ref == mdAssemblyRefNil {
            let cor_assembly_property = &module_metadata.cor_assembly_property;
            let assembly_metadata = ASSEMBLYMETADATA {
                usMajorVersion: cor_assembly_property.version.major,
                usMinorVersion: cor_assembly_property.version.minor,
                usBuildNumber: cor_assembly_property.version.build,
                usRevisionNumber: cor_assembly_property.version.revision,
                szLocale: std::ptr::null_mut(),
                cbLocale: 0,
                rProcessor: std::ptr::null_mut(),
                ulProcessor: 0,
                rOS: std::ptr::null_mut(),
                ulOS: 0,
            };

            self.cor_lib_assembly_ref = module_metadata.assembly_emit.define_assembly_ref(
                cor_assembly_property.public_key.bytes(),
                &cor_assembly_property.name,
                &assembly_metadata,
                &(cor_assembly_property.public_key.hash_algorithm().unwrap() as u32).to_le_bytes(),
                cor_assembly_property.assembly_flags,
            )?;
        }

        if self.object_type_ref == mdTypeRefNil {
            self.object_type_ref = module_metadata
                .emit
                .define_type_ref_by_name(self.cor_lib_assembly_ref, "System.Object")
                .map_err(|e| {
                    log::warn!("Could not define type_ref for System.Object");
                    e
                })?;
        }

        if self.ex_type_ref == mdTypeRefNil {
            self.ex_type_ref = module_metadata
                .emit
                .define_type_ref_by_name(self.cor_lib_assembly_ref, "System.Exception")
                .map_err(|e| {
                    log::warn!("Could not define type_ref for System.Exception");
                    e
                })?;
        }

        if self.type_ref == mdTypeRefNil {
            self.type_ref = module_metadata
                .emit
                .define_type_ref_by_name(self.cor_lib_assembly_ref, "System.Type")
                .map_err(|e| {
                    log::warn!("Could not define type_ref for System.Type");
                    e
                })?;
        }

        if self.runtime_type_handle_ref == mdTypeRefNil {
            self.runtime_type_handle_ref = module_metadata
                .emit
                .define_type_ref_by_name(self.cor_lib_assembly_ref, "System.RuntimeTypeHandle")
                .map_err(|e| {
                    log::warn!("Could not define type_ref for System.RuntimeTypeHandle");
                    e
                })?;
        }

        if self.get_type_from_handle_token == mdTokenNil {
            let mut runtime_type_handle_compressed =
                compress_token(self.runtime_type_handle_ref).unwrap();
            let mut type_ref_compressed = compress_token(self.type_ref).unwrap();

            let mut signature = Vec::with_capacity(
                4 + runtime_type_handle_compressed.len() + type_ref_compressed.len(),
            );
            signature.push(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_DEFAULT.bits());
            signature.push(1);
            signature.push(CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE);
            signature.append(&mut type_ref_compressed);
            signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as COR_SIGNATURE);
            signature.append(&mut runtime_type_handle_compressed);

            self.get_type_from_handle_token = module_metadata
                .emit
                .define_member_ref(self.type_ref, "GetTypeFromHandle", &signature)
                .map_err(|e| {
                    log::warn!("Could not define get_type_from_handle_token");
                    e
                })?;
        }

        if self.runtime_method_handle_ref == mdTypeRefNil {
            self.runtime_method_handle_ref = module_metadata
                .emit
                .define_type_ref_by_name(self.cor_lib_assembly_ref, "System.RuntimeMethodHandle")
                .map_err(|e| {
                    log::warn!("Could not define type_ref for System.RuntimeMethodHandle");
                    e
                })?;
        }

        Ok(())
    }

    pub fn ensure_base_calltarget_tokens(
        &mut self,
        module_metadata: &ModuleMetadata,
    ) -> Result<(), HRESULT> {
        self.ensure_cor_lib_tokens(module_metadata)?;

        if self.profiler_assembly_ref == mdAssemblyRefNil {
            let assembly_reference =
                crate::profiler::managed::MANAGED_PROFILER_FULL_ASSEMBLY_VERSION.deref();

            let (sz_locale, cb_locale) = if &assembly_reference.locale == "neutral" {
                (std::ptr::null_mut() as *mut WCHAR, 0)
            } else {
                let wstr = U16CString::from_str(&assembly_reference.locale).unwrap();
                let len = wstr.len() as ULONG;
                (wstr.into_vec().as_mut_ptr(), len)
            };

            let assembly_metadata = ASSEMBLYMETADATA {
                usMajorVersion: assembly_reference.version.major,
                usMinorVersion: assembly_reference.version.minor,
                usBuildNumber: assembly_reference.version.build,
                usRevisionNumber: assembly_reference.version.revision,
                szLocale: sz_locale,
                cbLocale: cb_locale,
                rProcessor: std::ptr::null_mut(),
                ulProcessor: 0,
                rOS: std::ptr::null_mut(),
                ulOS: 0,
            };

            let public_key_bytes = assembly_reference.public_key.into_bytes();
            self.profiler_assembly_ref = module_metadata
                .assembly_emit
                .define_assembly_ref(
                    &public_key_bytes,
                    &assembly_reference.name,
                    &assembly_metadata,
                    &[],
                    CorAssemblyFlags::empty(),
                )
                .map_err(|e| {
                    log::warn!("Could not define profiler_assembly_ref");
                    e
                })?;
        }

        if self.call_target_type_ref == mdTypeRefNil {
            self.call_target_type_ref = module_metadata
                .emit
                .define_type_ref_by_name(
                    self.profiler_assembly_ref,
                    managed::MANAGED_PROFILER_CALLTARGET_TYPE,
                )
                .map_err(|e| {
                    log::warn!(
                        "Could not define type_ref for {}",
                        managed::MANAGED_PROFILER_CALLTARGET_TYPE
                    );
                    e
                })?;
        }

        if self.call_target_state_type_ref == mdTypeRefNil {
            self.call_target_state_type_ref = module_metadata
                .emit
                .define_type_ref_by_name(
                    self.profiler_assembly_ref,
                    managed::MANAGED_PROFILER_CALLTARGET_STATETYPE,
                )
                .map_err(|e| {
                    log::warn!(
                        "Could not define type_ref for {}",
                        managed::MANAGED_PROFILER_CALLTARGET_STATETYPE
                    );
                    e
                })?;
        }

        if self.call_target_state_type_get_default == mdMemberRefNil {
            let mut call_target_state_type_compressed =
                compress_token(self.call_target_state_type_ref).unwrap();

            let mut signature = Vec::with_capacity(3 + call_target_state_type_compressed.len());
            signature.push(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_DEFAULT.bits());
            signature.push(0);
            signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as COR_SIGNATURE);
            signature.append(&mut call_target_state_type_compressed);

            self.call_target_state_type_get_default = module_metadata
                .emit
                .define_member_ref(
                    self.call_target_state_type_ref,
                    managed::MANAGED_PROFILER_CALLTARGET_STATETYPE_GETDEFAULT_NAME,
                    &signature,
                )
                .map_err(|e| {
                    log::warn!(
                        "Could not define member ref {}",
                        managed::MANAGED_PROFILER_CALLTARGET_STATETYPE_GETDEFAULT_NAME
                    );
                    e
                })?;
        }

        Ok(())
    }

    pub fn get_cor_lib_assembly_ref(&self) -> mdAssemblyRef {
        self.cor_lib_assembly_ref
    }

    pub fn get_object_type_ref(&self) -> mdTypeRef {
        self.object_type_ref
    }

    pub fn get_ex_type_ref(&self) -> mdTypeRef {
        self.ex_type_ref
    }

    pub fn get_target_state_type_ref(
        &mut self,
        module_metadata: &ModuleMetadata,
    ) -> Result<mdTypeRef, HRESULT> {
        self.ensure_base_calltarget_tokens(module_metadata)?;
        Ok(self.call_target_state_type_ref)
    }

    pub fn get_target_void_return_type_ref(
        &mut self,
        module_metadata: &ModuleMetadata,
    ) -> Result<mdTypeRef, HRESULT> {
        self.ensure_base_calltarget_tokens(module_metadata)?;
        if self.call_target_return_void_type_ref == mdTypeRefNil {
            self.call_target_return_void_type_ref = module_metadata
                .emit
                .define_type_ref_by_name(
                    self.profiler_assembly_ref,
                    managed::MANAGED_PROFILER_CALLTARGET_RETURNTYPE,
                )
                .map_err(|e| {
                    log::warn!(
                        "Could not define type_ref for {}",
                        managed::MANAGED_PROFILER_CALLTARGET_RETURNTYPE
                    );
                    e
                })?;
        }

        Ok(self.call_target_return_void_type_ref)
    }

    pub fn get_target_return_value_type_ref(
        &mut self,
        return_argument: &FunctionMethodArgument,
        module_metadata: &ModuleMetadata,
    ) -> Result<mdTypeSpec, HRESULT> {
        self.ensure_base_calltarget_tokens(module_metadata)?;

        if self.call_target_return_type_ref == mdTypeRefNil {
            self.call_target_return_type_ref = module_metadata
                .emit
                .define_type_ref_by_name(
                    self.profiler_assembly_ref,
                    managed::MANAGED_PROFILER_CALLTARGET_RETURNTYPE_GENERICS,
                )
                .map_err(|e| {
                    log::warn!(
                        "Could not define type_ref for {}",
                        managed::MANAGED_PROFILER_CALLTARGET_RETURNTYPE_GENERICS
                    );
                    e
                })?;
        }

        let return_signature = return_argument.signature();
        let mut call_target_return_type_ref_compressed =
            compress_token(self.call_target_return_type_ref).unwrap();

        let mut signature = Vec::with_capacity(
            3 + return_signature.len() + call_target_return_type_ref_compressed.len(),
        );
        signature.push(CorElementType::ELEMENT_TYPE_GENERICINST as COR_SIGNATURE);
        signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as COR_SIGNATURE);
        signature.append(&mut call_target_return_type_ref_compressed);
        signature.push(1);
        signature.extend_from_slice(return_signature);

        let return_value_type_spec = module_metadata.emit.get_token_from_type_spec(&signature)?;

        Ok(return_value_type_spec)
    }

    pub fn get_call_target_state_default_member_ref(
        &mut self,
        module_metadata: &ModuleMetadata,
    ) -> Result<mdMemberRef, HRESULT> {
        self.ensure_base_calltarget_tokens(module_metadata)?;
        Ok(self.call_target_state_type_get_default)
    }

    pub fn get_call_target_return_void_default_member_ref(
        &mut self,
        module_metadata: &ModuleMetadata,
    ) -> Result<mdMemberRef, HRESULT> {
        self.ensure_base_calltarget_tokens(module_metadata)?;

        if self.call_target_return_void_type_get_default == mdMemberRefNil {
            let call_target_return_void_type_compressed =
                compress_token(self.call_target_return_void_type_ref).unwrap();

            let mut signature =
                Vec::with_capacity(3 + call_target_return_void_type_compressed.len());
            signature.push(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_DEFAULT.bits());
            signature.push(0);
            signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as COR_SIGNATURE);
            signature.extend_from_slice(&call_target_return_void_type_compressed);

            self.call_target_return_void_type_get_default = module_metadata
                .emit
                .define_member_ref(
                    self.call_target_return_void_type_ref,
                    managed::MANAGED_PROFILER_CALLTARGET_RETURNTYPE_GETDEFAULT_NAME,
                    &signature,
                )
                .map_err(|e| {
                    log::warn!(
                        "Could not define member ref {}",
                        managed::MANAGED_PROFILER_CALLTARGET_RETURNTYPE_GETDEFAULT_NAME
                    );
                    e
                })?;
        }

        Ok(self.call_target_return_void_type_get_default)
    }

    pub fn get_call_target_return_value_default_member_ref(
        &mut self,
        call_target_return_type_spec: mdTypeSpec,
        module_metadata: &ModuleMetadata,
    ) -> Result<mdMemberRef, HRESULT> {
        self.ensure_base_calltarget_tokens(module_metadata)?;

        if self.call_target_return_type_ref == mdTypeRefNil {
            log::warn!("Could not define call_target_return_type_get_default because call_target_return_type_ref is null");
            return Err(E_FAIL);
        }

        let mut call_target_return_type_compressed =
            compress_token(self.call_target_return_type_ref).unwrap();

        let mut signature = Vec::with_capacity(7 + call_target_return_type_compressed.len());
        signature.push(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_DEFAULT.bits());
        signature.push(0);
        signature.push(CorElementType::ELEMENT_TYPE_GENERICINST as COR_SIGNATURE);
        signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as COR_SIGNATURE);
        signature.append(&mut call_target_return_type_compressed);
        signature.push(1);
        signature.push(CorElementType::ELEMENT_TYPE_VAR as COR_SIGNATURE);
        signature.push(0);

        module_metadata
            .emit
            .define_member_ref(
                call_target_return_type_spec,
                managed::MANAGED_PROFILER_CALLTARGET_RETURNTYPE_GETDEFAULT_NAME,
                &signature,
            )
            .map_err(|e| {
                log::warn!(
                    "Could not define member ref {}, {}",
                    managed::MANAGED_PROFILER_CALLTARGET_RETURNTYPE_GETDEFAULT_NAME,
                    e
                );
                e
            })
    }

    pub fn get_call_target_default_value_method_spec(
        &mut self,
        method_argument: &FunctionMethodArgument,
        module_metadata: &ModuleMetadata,
    ) -> Result<mdMethodSpec, HRESULT> {
        self.ensure_base_calltarget_tokens(module_metadata)?;

        if self.get_default_member_ref == mdMemberRefNil {
            let signature = vec![
                CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERIC.bits(),
                1,
                0,
                CorElementType::ELEMENT_TYPE_MVAR as COR_SIGNATURE,
                0,
            ];

            self.get_default_member_ref = module_metadata
                .emit
                .define_member_ref(
                    self.call_target_type_ref,
                    managed::MANAGED_PROFILER_CALLTARGET_GETDEFAULTVALUE_NAME,
                    &signature,
                )
                .map_err(|e| {
                    log::warn!(
                        "Could not define member ref {}",
                        managed::MANAGED_PROFILER_CALLTARGET_GETDEFAULTVALUE_NAME
                    );
                    e
                })?;
        }

        let method_argument_signature = method_argument.signature();
        let mut signature = Vec::with_capacity(2 + method_argument_signature.len());
        signature.push(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERICINST.bits());
        signature.push(1);
        signature.extend_from_slice(&method_argument_signature);

        let default_method_spec = module_metadata
            .emit
            .define_method_spec(self.get_default_member_ref, &signature)
            .map_err(|e| {
                log::warn!("Could not define default method spec");
                e
            })?;

        Ok(default_method_spec)
    }

    fn get_current_type_ref(&self, current_type: &MyTypeInfo) -> (mdToken, bool) {
        let mut is_value_type = current_type.is_value_type;
        if current_type.type_spec != mdTypeSpecNil {
            (current_type.type_spec, is_value_type)
        } else {
            let mut t = current_type;
            while !t.is_generic {
                if let Some(p) = &t.parent_type {
                    t = p;
                } else {
                    return (t.id, is_value_type);
                }
            }

            is_value_type = false;
            (self.object_type_ref, is_value_type)
        }
    }

    fn create_local_sig(
        &mut self,
        method: &Method,
        method_return_value: &FunctionMethodArgument,
        module_metadata: &ModuleMetadata,
    ) -> Result<LocalSig, HRESULT> {
        self.ensure_base_calltarget_tokens(module_metadata)?;

        let local_var_sig = method.header.local_var_sig_tok();
        let mut call_target_state_type_ref_compressed =
            compress_token(self.call_target_state_type_ref).unwrap();
        let mut original_sig = vec![];
        let mut original_signature_size = 0;

        if local_var_sig != mdTokenNil {
            original_sig = module_metadata.import.get_sig_from_token(local_var_sig)?;
            original_signature_size += original_sig.len();
            let offset = original_sig.len() - call_target_state_type_ref_compressed.len();
            if offset > 0
                && original_sig[offset - 1] == CorElementType::ELEMENT_TYPE_VALUETYPE as u8
                && original_sig[offset..] == call_target_state_type_ref_compressed
            {
                log::warn!("method signature has already been modified");
                return Err(E_FAIL);
            }
        }

        let mut new_locals_count: usize = 3;
        let mut ex_type_ref_compressed = compress_token(self.ex_type_ref).unwrap();
        let (_, ret_type_flags) = method_return_value.get_type_flags();

        let call_target_return;
        let mut call_target_return_signature = vec![];
        let mut call_target_return_compressed = vec![];
        let mut ret_sig = vec![];

        if ret_type_flags != MethodArgumentTypeFlag::VOID {
            ret_sig = method_return_value.signature().to_vec();
            call_target_return =
                self.get_target_return_value_type_ref(method_return_value, module_metadata)?;
            call_target_return_signature = {
                let type_spec = module_metadata.import.get_type_spec_from_token(
                    call_target_return
                ).map_err(|e| {
                    log::warn!("Could not get type spec from token, call_target_return={}, signature={:?}", call_target_return, &ret_sig);
                    e
                })?;
                type_spec.signature
            };

            new_locals_count += 1;
        } else {
            call_target_return = self.get_target_void_return_type_ref(module_metadata)?;
            call_target_return_compressed = compress_token(call_target_return).unwrap();
        }

        let mut new_locals_buffer;
        let mut old_locals_len = 0;

        if original_signature_size == 0 {
            new_locals_buffer = compress_data(new_locals_count as ULONG).unwrap();
        } else {
            let (data, len) = uncompress_data(&original_sig[1..]).unwrap();
            old_locals_len += len;
            new_locals_count += data as usize;
            new_locals_buffer = compress_data(new_locals_count as ULONG).unwrap();
        }

        let mut new_signature = vec![CorCallingConvention::IMAGE_CEE_CS_CALLCONV_LOCAL_SIG.bits()];
        new_signature.append(&mut new_locals_buffer);

        if original_signature_size > 0 {
            new_signature.extend_from_slice(&original_sig[(1 + old_locals_len)..]);
        }

        if !ret_sig.is_empty() {
            new_signature.extend_from_slice(&ret_sig);
        }

        new_signature.push(CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE);
        new_signature.append(&mut ex_type_ref_compressed);

        if !call_target_return_signature.is_empty() {
            new_signature.append(&mut call_target_return_signature);
        } else {
            new_signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as COR_SIGNATURE);
            new_signature.append(&mut call_target_return_compressed);
        }

        new_signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as COR_SIGNATURE);
        new_signature.append(&mut call_target_state_type_ref_compressed);

        let new_local_var_sig = module_metadata
            .emit
            .get_token_from_sig(&new_signature)
            .map_err(|e| {
                log::warn!(
                    "Error creating new local vars signature {:?}",
                    &new_signature
                );
                e
            })?;

        Ok(LocalSig {
            new_local_var_sig,
            call_target_state_token: self.call_target_state_type_ref,
            exception_token: self.ex_type_ref,
            call_target_return_token: call_target_return,
            return_value_index: if !ret_sig.is_empty() {
                new_locals_count - 4
            } else {
                usize::MAX
            },
            exception_index: new_locals_count - 3,
            call_target_return_index: new_locals_count - 2,
            call_target_state_index: new_locals_count - 1,
        })
    }

    pub fn write_begin_method_with_arguments_array(
        &mut self,
        integration_type_ref: mdTypeRef,
        current_type: &MyTypeInfo,
        module_metadata: &ModuleMetadata,
    ) -> Result<Instruction, HRESULT> {
        self.ensure_base_calltarget_tokens(module_metadata)?;

        if self.begin_array_member_ref == mdMemberRefNil {
            let call_target_state_compressed =
                compress_token(self.call_target_state_type_ref).unwrap();

            let mut signature = Vec::with_capacity(8 + call_target_state_compressed.len());
            signature.push(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERIC.bits());
            signature.push(2);
            signature.push(2);
            signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as COR_SIGNATURE);
            signature.extend_from_slice(&call_target_state_compressed);
            signature.push(CorElementType::ELEMENT_TYPE_MVAR as COR_SIGNATURE);
            signature.push(1);
            signature.push(CorElementType::ELEMENT_TYPE_SZARRAY as COR_SIGNATURE);
            signature.push(CorElementType::ELEMENT_TYPE_OBJECT as COR_SIGNATURE);

            self.begin_array_member_ref = module_metadata
                .emit
                .define_member_ref(
                    self.call_target_type_ref,
                    managed::MANAGED_PROFILER_CALLTARGET_BEGINMETHOD_NAME,
                    &signature,
                )
                .map_err(|e| {
                    log::warn!(
                        "Could not define member ref {}",
                        managed::MANAGED_PROFILER_CALLTARGET_BEGINMETHOD_NAME
                    );
                    e
                })?;
        }

        let integration_type_compressed = compress_token(integration_type_ref).unwrap();
        let (current_type_ref, is_value_type) = self.get_current_type_ref(current_type);
        let current_type_compressed = compress_token(current_type_ref).unwrap();

        let mut signature = Vec::with_capacity(
            4 + integration_type_compressed.len() + current_type_compressed.len(),
        );
        signature.push(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERICINST.bits());
        signature.push(2);
        signature.push(CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE);
        signature.extend_from_slice(&integration_type_compressed);

        if is_value_type {
            signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as COR_SIGNATURE);
        } else {
            signature.push(CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE);
        }

        signature.extend_from_slice(&current_type_compressed);

        let begin_array_method_spec = module_metadata
            .emit
            .define_method_spec(self.begin_array_member_ref, &signature)
            .map_err(|e| {
                log::warn!(
                    "Could not define method spec for {}",
                    managed::MANAGED_PROFILER_CALLTARGET_BEGINMETHOD_NAME
                );
                e
            })?;

        Ok(Instruction::call(begin_array_method_spec))
    }

    pub fn modify_local_sig_and_initialize(
        &mut self,
        method: &Method,
        function_info: &MyFunctionInfo,
        module_metadata: &ModuleMetadata,
    ) -> Result<(LocalSig, Vec<Instruction>), HRESULT> {
        // TODO: cache the parsed method in method_signature...
        let parsed_method = function_info.method_signature.try_parse().unwrap();
        let return_function_method = parsed_method.return_type();

        let local_sig = self.create_local_sig(method, &return_function_method, module_metadata)?;
        let mut instructions = Vec::with_capacity(6);

        if local_sig.return_value_index != usize::MAX {
            let call_target_default_value = self.get_call_target_default_value_method_spec(
                &return_function_method,
                module_metadata,
            )?;

            instructions.push(Instruction::call(call_target_default_value));
            instructions.push(Instruction::store_local(
                local_sig.return_value_index as u16,
            ));

            let call_target_return_value = self.get_call_target_return_value_default_member_ref(
                local_sig.call_target_return_token,
                module_metadata,
            )?;

            instructions.push(Instruction::call(call_target_return_value));
            instructions.push(Instruction::store_local(
                local_sig.call_target_return_index as u16,
            ));
        } else {
            let call_target_void =
                self.get_call_target_return_void_default_member_ref(module_metadata)?;
            instructions.push(Instruction::call(call_target_void));
            instructions.push(Instruction::store_local(
                local_sig.call_target_return_index as u16,
            ));
        }

        instructions.push(Instruction::ldnull());
        instructions.push(Instruction::store_local(local_sig.exception_index as u16));
        Ok((local_sig, instructions))
    }

    pub fn write_begin_method(
        &mut self,
        integration_type_ref: mdTypeRef,
        current_type: &MyTypeInfo,
        method_arguments: &[FunctionMethodArgument],
        module_metadata: &ModuleMetadata,
    ) -> Result<Instruction, HRESULT> {
        self.ensure_base_calltarget_tokens(module_metadata)?;

        let len = method_arguments.len();
        if len >= Self::FAST_PATH_COUNT {
            // slow path
            return self.write_begin_method_with_arguments_array(
                integration_type_ref,
                current_type,
                module_metadata,
            );
        }

        // fast path
        if self.begin_method_fast_path_refs[len] == mdMemberRefNil {
            let call_target_state = compress_token(self.call_target_state_type_ref).unwrap();

            let mut signature = Vec::with_capacity(6 + (len * 2) + call_target_state.len());
            signature.push(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERIC.bits());
            signature.push(2 + len as u8);
            signature.push(1 + len as u8);
            signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as COR_SIGNATURE);
            signature.extend_from_slice(&call_target_state);
            signature.push(CorElementType::ELEMENT_TYPE_MVAR as COR_SIGNATURE);
            signature.push(1);

            for i in 0..len {
                signature.push(CorElementType::ELEMENT_TYPE_MVAR as COR_SIGNATURE);
                signature.push(1 + (i as u8 + 1));
            }

            self.begin_method_fast_path_refs[len] = module_metadata
                .emit
                .define_member_ref(
                    self.call_target_type_ref,
                    managed::MANAGED_PROFILER_CALLTARGET_BEGINMETHOD_NAME,
                    &signature,
                )
                .map_err(|e| {
                    log::warn!(
                        "Could not define member ref {}",
                        managed::MANAGED_PROFILER_CALLTARGET_BEGINMETHOD_NAME
                    );
                    e
                })?
        }

        let integration_type_ref_compressed = compress_token(integration_type_ref).unwrap();

        let (current_type_ref, is_value_type) = self.get_current_type_ref(current_type);

        let current_type_ref_compressed = compress_token(current_type_ref).unwrap();

        let mut arguments_signature = Vec::with_capacity(Self::FAST_PATH_COUNT);
        for method_argument in method_arguments {
            let sig = method_argument.signature();
            arguments_signature.extend_from_slice(sig);
        }

        let mut signature = Vec::with_capacity(
            4 + integration_type_ref_compressed.len()
                + current_type_ref_compressed.len()
                + arguments_signature.len(),
        );
        signature.push(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERICINST.bits());
        signature.push(2 + len as u8);
        signature.push(CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE);
        signature.extend_from_slice(&integration_type_ref_compressed);

        if is_value_type {
            signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as COR_SIGNATURE);
        } else {
            signature.push(CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE);
        }

        signature.extend_from_slice(&current_type_ref_compressed);
        signature.extend_from_slice(&arguments_signature);

        let begin_method_spec = module_metadata
            .emit
            .define_method_spec(self.begin_method_fast_path_refs[len], &signature)
            .map_err(|e| {
                log::warn!("Could not define member spec for fast path args {}", len);
                e
            })?;

        Ok(Instruction::call(begin_method_spec))
    }

    pub fn write_end_void_return_member_ref(
        &mut self,
        integration_type_ref: mdTypeRef,
        current_type: &MyTypeInfo,
        module_metadata: &ModuleMetadata,
    ) -> Result<Instruction, HRESULT> {
        self.ensure_base_calltarget_tokens(module_metadata)?;

        if self.end_void_member_ref == mdMemberRefNil {
            let mut call_target_return_void_compressed =
                compress_token(self.call_target_return_void_type_ref).unwrap();

            let mut ex_type_ref_compressed = compress_token(self.ex_type_ref).unwrap();

            let mut call_target_state = compress_token(self.call_target_state_type_ref).unwrap();

            let mut signature = Vec::with_capacity(
                8 + call_target_return_void_compressed.len()
                    + ex_type_ref_compressed.len()
                    + call_target_state.len(),
            );

            signature.push(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERIC.bits());
            signature.push(2);
            signature.push(3);
            signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as u8);
            signature.append(&mut call_target_return_void_compressed);

            signature.push(CorElementType::ELEMENT_TYPE_MVAR as u8);
            signature.push(1);
            signature.push(CorElementType::ELEMENT_TYPE_CLASS as u8);
            signature.append(&mut ex_type_ref_compressed);

            signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as u8);
            signature.append(&mut call_target_state);

            self.end_void_member_ref = module_metadata
                .emit
                .define_member_ref(
                    self.call_target_type_ref,
                    managed::MANAGED_PROFILER_CALLTARGET_ENDMETHOD_NAME,
                    &signature,
                )
                .map_err(|e| {
                    log::warn!(
                        "Could not define member ref {}",
                        managed::MANAGED_PROFILER_CALLTARGET_ENDMETHOD_NAME
                    );
                    e
                })?;
        }

        let mut integration_type_ref_compressed = compress_token(integration_type_ref).unwrap();

        let (current_type_ref, is_value_type) = self.get_current_type_ref(current_type);

        let mut current_type_ref_compressed = compress_token(current_type_ref).unwrap();

        let mut signature = Vec::with_capacity(
            4 + integration_type_ref_compressed.len() + current_type_ref_compressed.len(),
        );
        signature.push(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERICINST.bits());
        signature.push(2);
        signature.push(CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE);
        signature.append(&mut integration_type_ref_compressed);

        if is_value_type {
            signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as COR_SIGNATURE);
        } else {
            signature.push(CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE);
        }

        signature.append(&mut current_type_ref_compressed);

        let end_void_method_spec = module_metadata
            .emit
            .define_method_spec(self.end_void_member_ref, &signature)
            .map_err(|e| {
                log::warn!("Could not define member spec for end void method");
                e
            })?;

        Ok(Instruction::call(end_void_method_spec))
    }

    pub fn write_end_return_member_ref(
        &mut self,
        integration_type_ref: mdTypeRef,
        current_type: &MyTypeInfo,
        return_argument: &FunctionMethodArgument,
        module_metadata: &ModuleMetadata,
    ) -> Result<Instruction, HRESULT> {
        let return_type_spec =
            self.get_target_return_value_type_ref(return_argument, module_metadata)?;

        let mut call_target_return_type_compressed =
            compress_token(self.call_target_return_type_ref).unwrap();

        let mut ex_type_ref_compressed = compress_token(self.ex_type_ref).unwrap();

        let mut call_target_state = compress_token(self.call_target_state_type_ref).unwrap();

        let mut signature = Vec::with_capacity(
            14 + call_target_return_type_compressed.len()
                + ex_type_ref_compressed.len()
                + call_target_state.len(),
        );
        signature.push(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERIC.bits());
        signature.push(3);
        signature.push(4);
        signature.push(CorElementType::ELEMENT_TYPE_GENERICINST as COR_SIGNATURE);
        signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as COR_SIGNATURE);
        signature.append(&mut call_target_return_type_compressed);
        signature.push(1);
        signature.push(CorElementType::ELEMENT_TYPE_MVAR as COR_SIGNATURE);
        signature.push(2);

        signature.push(CorElementType::ELEMENT_TYPE_MVAR as COR_SIGNATURE);
        signature.push(1);

        signature.push(CorElementType::ELEMENT_TYPE_MVAR as COR_SIGNATURE);
        signature.push(2);

        signature.push(CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE);
        signature.append(&mut ex_type_ref_compressed);

        signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as COR_SIGNATURE);
        signature.append(&mut call_target_state);

        let end_method_member_ref = module_metadata
            .emit
            .define_member_ref(
                self.call_target_type_ref,
                managed::MANAGED_PROFILER_CALLTARGET_ENDMETHOD_NAME,
                &signature,
            )
            .map_err(|e| {
                log::warn!(
                    "Could not define member ref {}",
                    managed::MANAGED_PROFILER_CALLTARGET_ENDMETHOD_NAME
                );
                e
            })?;

        let mut integration_type_ref_compressed = compress_token(integration_type_ref).unwrap();

        let (current_type_ref, is_value_type) = self.get_current_type_ref(current_type);

        let mut current_type_ref_compressed = compress_token(current_type_ref).unwrap();

        let ret_type_sig = return_argument.signature();

        let mut signature = Vec::with_capacity(
            4 + integration_type_ref_compressed.len()
                + current_type_ref_compressed.len()
                + ret_type_sig.len(),
        );

        signature.push(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERICINST.bits());
        signature.push(3);
        signature.push(CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE);
        signature.append(&mut integration_type_ref_compressed);

        if is_value_type {
            signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as COR_SIGNATURE);
        } else {
            signature.push(CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE);
        }

        signature.append(&mut current_type_ref_compressed);
        signature.extend_from_slice(ret_type_sig);

        let end_method_spec = module_metadata
            .emit
            .define_method_spec(end_method_member_ref, &signature)
            .map_err(|e| {
                log::warn!("Could not define member spec for end method");
                e
            })?;

        Ok(Instruction::call(end_method_spec))
    }

    pub fn write_log_exception(
        &mut self,
        integration_type_ref: mdTypeRef,
        current_type: &MyTypeInfo,
        module_metadata: &ModuleMetadata,
    ) -> Result<Instruction, HRESULT> {
        self.ensure_base_calltarget_tokens(module_metadata)?;

        if self.log_exception_ref == mdMemberRefNil {
            let mut ex_type_ref = compress_token(self.ex_type_ref).unwrap();
            let mut signature = Vec::with_capacity(5 + ex_type_ref.len());
            signature.push(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERIC.bits());
            signature.push(2);
            signature.push(1);
            signature.push(CorElementType::ELEMENT_TYPE_VOID as COR_SIGNATURE);
            signature.push(CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE);
            signature.append(&mut ex_type_ref);

            self.log_exception_ref = module_metadata
                .emit
                .define_member_ref(
                    self.call_target_type_ref,
                    managed::MANAGED_PROFILER_CALLTARGET_LOGEXCEPTION_NAME,
                    &signature,
                )
                .map_err(|e| {
                    log::warn!(
                        "Could not define member ref {}",
                        managed::MANAGED_PROFILER_CALLTARGET_LOGEXCEPTION_NAME
                    );
                    e
                })?;
        }

        let mut integration_type_ref_compressed = compress_token(integration_type_ref).unwrap();

        let (current_type_ref, is_value_type) = self.get_current_type_ref(current_type);

        let mut current_type_ref_compressed = compress_token(current_type_ref).unwrap();
        let mut signature = Vec::with_capacity(
            4 + integration_type_ref_compressed.len() + current_type_ref_compressed.len(),
        );

        signature.push(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERICINST.bits());
        signature.push(2);
        signature.push(CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE);
        signature.append(&mut integration_type_ref_compressed);

        if is_value_type {
            signature.push(CorElementType::ELEMENT_TYPE_VALUETYPE as COR_SIGNATURE);
        } else {
            signature.push(CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE);
        }

        signature.append(&mut current_type_ref_compressed);

        let log_exception_method_spec = module_metadata
            .emit
            .define_method_spec(self.log_exception_ref, &signature)
            .map_err(|e| {
                log::warn!("Could not define member spec for log exception method");
                e
            })?;

        Ok(Instruction::call(log_exception_method_spec))
    }

    pub fn write_call_target_return_get_return_value(
        &mut self,
        call_target_return_type_spec: mdTypeSpec,
        module_metadata: &ModuleMetadata,
    ) -> Result<Instruction, HRESULT> {
        self.ensure_base_calltarget_tokens(module_metadata)?;

        let signature = vec![
            (CorCallingConvention::IMAGE_CEE_CS_CALLCONV_DEFAULT
                | CorCallingConvention::IMAGE_CEE_CS_CALLCONV_HASTHIS)
                .bits(),
            0,
            CorElementType::ELEMENT_TYPE_VAR as COR_SIGNATURE,
            0,
        ];

        let call_target_return_get_value_member_ref = module_metadata
            .emit
            .define_member_ref(
                call_target_return_type_spec,
                managed::MANAGED_PROFILER_CALLTARGET_RETURNTYPE_GETRETURNVALUE_NAME,
                &signature,
            )
            .map_err(|e| {
                log::warn!(
                    "Could not define member ref {}",
                    managed::MANAGED_PROFILER_CALLTARGET_RETURNTYPE_GETRETURNVALUE_NAME
                );
                e
            })?;

        Ok(Instruction::call(call_target_return_get_value_member_ref))
    }
}

/// Metadata about a local signature
#[derive(Debug)]
pub struct LocalSig {
    pub new_local_var_sig: mdToken,
    pub call_target_state_token: mdTypeRef,
    pub exception_token: mdTypeRef,
    pub call_target_return_token: mdToken,
    pub return_value_index: usize,
    pub exception_index: usize,
    pub call_target_return_index: usize,
    pub call_target_state_index: usize,
}
