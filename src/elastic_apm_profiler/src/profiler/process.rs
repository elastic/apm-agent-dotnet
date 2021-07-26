use crate::{
    cli::{
        uncompress_token, Instruction, Method, Operand::InlineMethod, Section, CALL, CALLVIRT,
        CONSTRAINED,
    },
    ffi::{
        mdMemberRefNil, mdToken, mdTypeRefNil, CorElementType, FunctionID, ModuleID, E_FAIL, ULONG,
    },
    interfaces::ICorProfilerInfo4,
    profiler,
    profiler::{
        env, helpers,
        helpers::return_type_is_value_type_or_generic,
        sig::{parse_signature_types, parse_type},
        types::{MetadataBuilder, MethodReplacement, ModuleMetadata, WrapperMethodReference},
    },
    types::{MyFunctionInfo, WrapperMethodRef},
};
use com::sys::HRESULT;
use log::Level;
use num_traits::FromPrimitive;
use std::mem::transmute;

pub fn process_insertion_calls(
    profiler_info: &ICorProfilerInfo4,
    module_metadata: &mut ModuleMetadata,
    function_id: FunctionID,
    module_id: ModuleID,
    function_token: mdToken,
    caller: &MyFunctionInfo,
    method_replacements: &[MethodReplacement],
) -> Result<(), HRESULT> {
    // TODO: implement

    Ok(())
}

pub fn process_replacement_calls(
    profiler_info: &ICorProfilerInfo4,
    module_metadata: &mut ModuleMetadata,
    function_id: FunctionID,
    module_id: ModuleID,
    function_token: mdToken,
    caller: &MyFunctionInfo,
    method_replacements: &[MethodReplacement],
) -> Result<(), HRESULT> {
    let il_body = profiler_info.get_il_function_body(module_id, function_token)?;
    let mut method = Method::new(il_body.into()).map_err(|e| {
        log::warn!("process_replacement_calls: error decoding il. {:?}", e);
        E_FAIL
    })?;

    let mut original_il = None;
    let mut modified = false;
    for method_replacement in method_replacements {
        if method_replacement.wrapper.is_none() {
            continue;
        }
        let wrapper = method_replacement.wrapper.as_ref().unwrap();
        if &wrapper.action != "ReplaceTargetMethod" {
            continue;
        }

        let wrapper_method_key = wrapper.get_method_cache_key();
        if module_metadata.is_failed_wrapper_member_key(&wrapper_method_key) {
            continue;
        }

        let instructions = method.instructions.clone();
        let mut index_mod = 0;
        for (mut instr_index, instruction) in instructions.iter().enumerate() {
            if instruction.opcode != CALL && instruction.opcode != CALLVIRT {
                continue;
            }

            let original_argument;
            if let InlineMethod(token) = &instruction.operand {
                original_argument = *token;
            } else {
                continue;
            }

            let target = module_metadata.import.get_function_info(original_argument);
            if target.is_err() {
                continue;
            }
            let target = target.unwrap();

            if method_replacement.target.as_ref().map_or(true, |t| {
                target
                    .type_info
                    .as_ref()
                    .map_or(true, |tt| tt.name != t.type_name)
                    || t.method_name != target.name
            }) {
                continue;
            }

            if wrapper.method_signature.is_none() {
                continue;
            }

            let added_parameters_count = 3;
            let wrapper_method_signature = wrapper.method_signature.as_ref().unwrap();
            if wrapper_method_signature.len() < (added_parameters_count + 3) as usize {
                log::warn!(
                    "skipping because wrapper signature length {} does not match expected {}",
                    wrapper_method_signature.len(),
                    added_parameters_count + 3
                );
                continue;
            }

            let mut expected_number_args = wrapper_method_signature.arguments_len();
            expected_number_args -= added_parameters_count;

            let target_signature = &target.signature;
            if target_signature.is_instance_method() {
                expected_number_args -= 1;
            }

            let target_arg_count = target_signature.arguments_len();
            if expected_number_args != target_arg_count {
                log::warn!(
                    "skipping {} because expected argument length {} does not match actual {}",
                    caller.full_name(),
                    expected_number_args,
                    target_arg_count
                );
                continue;
            }

            let mut generated_wrapper_method_ref = match get_wrapper_method_ref(
                profiler_info,
                module_metadata,
                module_id,
                wrapper,
                &wrapper_method_key,
            ) {
                Ok(g) => g,
                Err(_) => {
                    log::warn!(
                        "JITCompilationStarted failed to obtain wrapper method ref for {}.{}(). function_id={}, function_token={}, name={}()",
                        &wrapper.type_name,
                        wrapper.method_name.as_ref().map_or("", |m| m.as_str()),
                        function_id,
                        function_token,
                        caller.full_name()
                    );
                    continue;
                }
            };

            let mut method_def_md_token = target.id;
            if target.is_generic {
                if target_signature.type_arguments_len()
                    != wrapper_method_signature.type_arguments_len()
                {
                    continue;
                }

                // we need to emit a method spec to populate the generic arguments
                generated_wrapper_method_ref.method_ref =
                    module_metadata.emit.define_method_spec(
                        generated_wrapper_method_ref.method_ref,
                        target.function_spec_signature.as_ref().unwrap().bytes(),
                    ).map_err(|e| {
                        log::warn!("JITCompilationStarted: error defining method spec. function_id={}, function_token={}, name={}()",
                                   function_id,
                                   function_token,
                                   target.full_name());
                        e
                    })?;
                method_def_md_token = target.method_def_id;
            }

            let actual_sig = parse_signature_types(module_metadata, &target);
            if actual_sig.is_none() {
                if log::log_enabled!(log::Level::Debug) {
                    log::debug!(
                        "JITCompilationStarted: skipping function call, failed to parse signature. function_id={}, function_token={}, name={}()",
                        function_id,
                        function_token,
                        target.full_name());
                }

                continue;
            }

            let actual_sig = actual_sig.unwrap();
            let expected_sig = method_replacement
                .target
                .as_ref()
                .unwrap()
                .signature_types
                .as_ref()
                .unwrap();

            if actual_sig.len() != expected_sig.len() {
                if log::log_enabled!(log::Level::Debug) {
                    log::debug!("JITCompilationStarted: skipping function call, actual signature length does not match expected signature length. function_id={}, function_token={}, name={}(), expected_sig={:?}, actual_sig={:?}",
                                function_id,
                                function_token,
                                target.full_name(),
                                expected_sig,
                                &actual_sig
                    );
                }

                continue;
            }

            let mut mismatch = false;
            for (idx, expected) in expected_sig.iter().enumerate() {
                if expected != "_" && expected != &actual_sig[idx] {
                    if log::log_enabled!(log::Level::Debug) {
                        log::debug!("JITCompilationStarted: skipping function call, types don't match. function_id={}, function_token={}, name={}(), expected_sig[{}]={}, actual_sig[{}]={}",
                                    function_id,
                                    function_token,
                                    target.full_name(),
                                    idx,
                                    expected,
                                    idx,
                                    &actual_sig[idx]
                        );
                    }

                    mismatch = true;
                    break;
                }
            }

            if mismatch {
                continue;
            }

            if !profiler::profiler_assembly_loaded_in_app_domain(module_metadata.app_domain_id) {
                log::warn!(
                    "JITCompilationStarted: skipping method as replacement found but managed profiler \
                        has not been loaded into AppDomain id={}, function_id={}, function_token={}, caller_name={}(), target_name={}()",
                    module_metadata.app_domain_id,
                    function_id,
                    function_token,
                    caller.full_name(),
                    target.full_name()
                );

                continue;
            }

            if instruction.opcode == CALLVIRT && instructions[instr_index - 1].opcode == CONSTRAINED
            {
                log::warn!(
                    "JITCompilationStarted: skipping method as replacement found but target method is a constrained \
                        virtual method call, which is not supported. function_id={}, function_token={}, caller_name={}(), target_name={}()",
                    function_id,
                    function_token,
                    caller.full_name(),
                    target.full_name()
                );
                continue;
            }

             if *env::ELASTIC_APM_PROFILER_DISPLAY_IL {
                 original_il = Some(helpers::get_il_codes(
                    "IL original code for caller: ",
                    &method,
                    caller,
                    module_metadata,
                ));
            }

            // We're going to push instructions onto the stack which may exceed tiny format max stack,
            // so expand tiny format to fat format to ensure the stack will be big enough
            method.expand_tiny_to_fat();

            let original_opcode = instruction.opcode;

            // Replace the original call instruction with a nop
            method
                .replace(instr_index + index_mod, Instruction::nop())
                .map_err(|e| {
                    log::warn!("unable to replace instruction {:?}", e);
                    E_FAIL
                })?;

            // insert instructions after the original instruction
            instr_index += 1;

            let original_method_def = target.id;
            let argument_len = target_arg_count;
            let return_type_index = target_signature.index_of_return_type();
            let return_type_bytes = &target_signature.bytes()[return_type_index..];

            let mut signature_read_success = true;
            let mut idx = 0;
            for _ in 0..argument_len {
                if let Some(type_idx) = parse_type(&return_type_bytes[idx..]) {
                    idx += type_idx;
                } else {
                    signature_read_success = false;
                    break;
                }
            }

            let mut this_index_mod = 0;

            if signature_read_success {
                // handle CancellationToken in the last argument position
                if let Some(CorElementType::ELEMENT_TYPE_VALUETYPE) =
                    CorElementType::from_u8(return_type_bytes[idx])
                {
                    idx += 1;
                    let (value_type_token, len) = uncompress_token(&return_type_bytes[idx..]);
                    if len > 0 {
                        if let Ok(Some(type_info)) =
                            module_metadata.import.get_type_info(value_type_token)
                        {
                            if &type_info.name == "System.Threading.CancellationToken" {
                                method
                                    .insert(
                                        instr_index + index_mod,
                                        Instruction::box_(value_type_token),
                                    )
                                    .map_err(|e| {
                                        log::warn!(
                                            "error inserting box({}) instruction, {:?}",
                                            value_type_token,
                                            e
                                        );
                                        E_FAIL
                                    })?;

                                instr_index += 1;
                                this_index_mod += 1;
                            }
                        }
                    }
                }
                // handle ReadOnlyMemory<T> in the last argument position
                else if let Some(CorElementType::ELEMENT_TYPE_GENERICINST) =
                    CorElementType::from_u8(return_type_bytes[idx])
                {
                    let start_idx = idx;
                    let mut end_idx = start_idx;

                    idx += 1;
                    if let Some(CorElementType::ELEMENT_TYPE_VALUETYPE) =
                        CorElementType::from_u8(return_type_bytes[idx])
                    {
                        idx += 1;
                        let (value_type_token, len) = uncompress_token(&return_type_bytes[idx..]);
                        if len > 0 {
                            if let Ok(Some(type_info)) =
                                module_metadata.import.get_type_info(value_type_token)
                            {
                                if &type_info.name == "System.ReadOnlyMemory`1" {
                                    if let Some(type_idx) =
                                        parse_type(&return_type_bytes[end_idx..])
                                    {
                                        end_idx += type_idx;
                                        if let Ok(type_token) =
                                            module_metadata.emit.get_token_from_type_spec(
                                                &return_type_bytes[start_idx..end_idx],
                                            )
                                        {
                                            method
                                                .insert(
                                                    instr_index + index_mod,
                                                    Instruction::box_(type_token),
                                                )
                                                .map_err(|e| {
                                                    log::warn!(
                                                        "error inserting box({}) instruction, {:?}",
                                                        type_token,
                                                        e
                                                    );
                                                    E_FAIL
                                                })?;

                                            instr_index += 1;
                                            this_index_mod += 1;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // TODO: add a fn to method insert all instructions in one go
            method
                .insert(
                    instr_index + index_mod,
                    Instruction::call(generated_wrapper_method_ref.method_ref),
                )
                .map_err(|e| {
                    log::warn!(
                        "error inserting call({}) instruction, {:?}",
                        generated_wrapper_method_ref.method_ref,
                        e
                    );
                    E_FAIL
                })?;

            method
                .insert(
                    instr_index + index_mod,
                    Instruction::load_int32(original_opcode.byte_2 as i32),
                )
                .map_err(|e| {
                    log::warn!(
                        "error inserting load_int32({}) instruction, {:?}",
                        original_opcode.byte_2,
                        e
                    );
                    E_FAIL
                })?;

            method
                .insert(
                    instr_index + 1 + index_mod,
                    Instruction::load_int32(method_def_md_token as i32),
                )
                .map_err(|e| {
                    log::warn!(
                        "error inserting load_int32({}) instruction, {:?}",
                        method_def_md_token,
                        e
                    );
                    E_FAIL
                })?;

            let module_ptr: i64 = unsafe { transmute(&module_metadata.module_version_id) };
            method
                .insert(instr_index + 2 + index_mod, Instruction::ldc_i8(module_ptr))
                .map_err(|e| {
                    log::warn!(
                        "error inserting ldc_i8({}) instruction, {:?}",
                        module_ptr,
                        e
                    );
                    E_FAIL
                })?;

            this_index_mod += 4;

            if wrapper_method_signature.return_type_is_object() {
                if let Some(type_token) = return_type_is_value_type_or_generic(
                    &module_metadata.import,
                    &module_metadata.emit,
                    &module_metadata.assembly_emit,
                    target.id,
                    &target.signature,
                ) {
                    if log::log_enabled!(Level::Debug) {
                        log::debug!(
                            "JITCompilationStarted: insert 'unbox.any' {} instruction after \
                            calling target function. function_id={} token={} target_name={}()",
                            type_token,
                            function_id,
                            function_token,
                            target.full_name()
                        );
                    }

                    method
                        .insert(
                            instr_index + 4 + index_mod,
                            Instruction::unbox_any(type_token),
                        )
                        .map_err(|e| {
                            log::warn!("error inserting unbox.any instruction, {:?}", e);
                            E_FAIL
                        })?;

                    this_index_mod += 1;
                }
            }

            index_mod += this_index_mod;

            modified = true;
            log::info!(
                "JITCompilationStarted: replaced calls from {}() to {}() with calls to {}() {}",
                caller.full_name(),
                target.full_name(),
                wrapper.full_name(),
                generated_wrapper_method_ref.method_ref
            );
        }
    }

    if modified {
        if *env::ELASTIC_APM_PROFILER_DISPLAY_IL {
            let modified_il = helpers::get_il_codes(
                "IL modification for caller: ",
                &method,
                caller,
                module_metadata,
            );
            log::debug!("{}\n{}", original_il.unwrap_or_default(), modified_il);
        }

        let method_bytes = method.into_bytes();
        let allocator = profiler_info.get_il_function_body_allocator(module_id)?;
        let allocated_bytes = allocator.alloc(method_bytes.len() as ULONG).map_err(|e| {
            log::warn!("process_replacement_calls: failed to allocate memory for replacement il");
            e
        })?;

        let address = unsafe { allocated_bytes.into_inner() };
        unsafe {
            std::ptr::copy(method_bytes.as_ptr(), address, method_bytes.len());
        }
        profiler_info
            .set_il_function_body(module_id, function_token, address as *const _)
            .map_err(|e| {
                log::warn!(
                    "process_replacement_calls: failed to set il for module_id={} {}",
                    module_id,
                    function_token
                );
                e
            })?;
    }

    Ok(())
}

pub fn get_wrapper_method_ref(
    profiler_info: &ICorProfilerInfo4,
    module_metadata: &mut ModuleMetadata,
    module_id: ModuleID,
    wrapper: &WrapperMethodReference,
    wrapper_method_key: &str,
) -> Result<WrapperMethodRef, HRESULT> {
    let wrapper_type_key = wrapper.get_type_cache_key();
    if let Some(method_ref) = module_metadata.get_wrapper_member_ref(wrapper_method_key) {
        let type_ref = module_metadata
            .get_wrapper_parent_type_ref(&wrapper_type_key)
            .unwrap_or(mdTypeRefNil);
        return Ok(WrapperMethodRef {
            type_ref,
            method_ref,
        });
    }

    let module_info = profiler_info.get_module_info(module_id)?;
    let module = module_metadata
        .import
        .get_module_from_scope()
        .map_err(|e| {
            log::warn!(
                "JITCompilationStarted failed to get module for module_id={} module_name={}",
                module_id,
                &module_info.file_name
            );
            e
        })?;

    let mut metadata_builder = MetadataBuilder::new(module_metadata, module);

    metadata_builder
        .emit_assembly_ref(&wrapper.assembly)
        .map_err(|e| {
            log::warn!(
                "JITCompilationStarted: failed to emit wrapper assembly ref for assembly={}",
                &wrapper.assembly
            );
            e
        })?;

    metadata_builder
        .store_wrapper_method_ref(wrapper)
        .map_err(|e| {
            log::warn!(
                "JITCompilationStarted: failed to store wrapper method ref for {}.{}()",
                &wrapper.type_name,
                wrapper.method_name.as_ref().map_or("", |m| m.as_str())
            );
            e
        })?;

    let method_ref = module_metadata
        .get_wrapper_member_ref(&wrapper_method_key)
        .unwrap_or(mdMemberRefNil);
    let type_ref = module_metadata
        .get_wrapper_parent_type_ref(&wrapper_type_key)
        .unwrap_or(mdTypeRefNil);

    Ok(WrapperMethodRef {
        type_ref,
        method_ref,
    })
}
