// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::{
    cil::{
        CorExceptionFlag, FatSectionClause, Instruction, Method, Operand::ShortInlineBrTarget, RET,
    },
    ffi::{
        mdMethodDef, mdTokenNil, mdTypeSpecNil, CorCallingConvention, FunctionID, ModuleID, ReJITID,
    },
    interfaces::{ICorProfilerFunctionControl, ICorProfilerInfo4},
    profiler::{
        calltarget_tokens::CallTargetTokens,
        env, helpers, process,
        types::{
            FunctionInfo, MethodArgumentTypeFlag, MethodReplacement, ModuleMetadata,
            ModuleWrapperTokens, TypeInfo,
        },
    },
};
use com::sys::{HRESULT, S_FALSE};
use log::Level;
use std::{
    collections::HashMap,
    sync::{
        mpsc::{channel, Sender},
        Mutex,
    },
    thread,
    thread::JoinHandle,
};

pub struct RejitHandlerModule {
    module_id: ModuleID,
    method_defs: HashMap<mdMethodDef, RejitHandlerModuleMethod>,
    method_defs_mutex: Mutex<()>,
}

impl RejitHandlerModule {
    pub fn new(module_id: ModuleID) -> Self {
        Self {
            module_id,
            method_defs: HashMap::new(),
            method_defs_mutex: Mutex::new(()),
        }
    }

    pub fn get_or_add_method(&mut self, method_def: mdMethodDef) -> &mut RejitHandlerModuleMethod {
        let _lock = self.method_defs_mutex.lock().unwrap();
        self.method_defs
            .entry(method_def)
            .or_insert_with(|| RejitHandlerModuleMethod::new(method_def))
    }
}

pub struct RejitHandlerModuleMethod {
    method_def: mdMethodDef,
    function_info: Option<FunctionInfo>,
    method_replacement: Option<MethodReplacement>,
}

impl RejitHandlerModuleMethod {
    pub fn new(method_def: mdMethodDef) -> Self {
        Self {
            method_def,
            function_info: None,
            method_replacement: None,
        }
    }

    pub fn set_function_info(&mut self, function_info: FunctionInfo) {
        self.function_info = Some(function_info);
    }

    pub fn set_method_replacement(&mut self, method_replacement: MethodReplacement) {
        self.method_replacement = Some(method_replacement);
    }

    pub fn function_info(&self) -> Option<&FunctionInfo> {
        self.function_info.as_ref()
    }
}

#[derive(Debug)]
struct RejitItem {
    module_ids: Vec<ModuleID>,
    method_ids: Vec<mdMethodDef>,
}

pub struct RejitHandler {
    sender: Sender<RejitItem>,
    handle: JoinHandle<()>,
    modules: HashMap<ModuleID, RejitHandlerModule>,
    modules_mutex: Mutex<()>,
}

impl RejitHandler {
    pub fn new(profiler_info: ICorProfilerInfo4) -> Self {
        let (sender, receiver) = channel::<RejitItem>();
        let handle = thread::spawn(move || {
            // initialize the current thread in advance of rejit calls
            match profiler_info.initialize_current_thread() {
                Ok(_) => {
                    while let Ok(item) = receiver.recv() {
                        match profiler_info.request_rejit(&item.module_ids, &item.method_ids) {
                            Ok(_) => {
                                log::info!(
                                    "request ReJIT done for {} methods",
                                    item.method_ids.len()
                                );
                            }
                            Err(e) => {
                                log::warn!(
                                    "error requesting ReJIT for {} methods. {}",
                                    item.method_ids.len(),
                                    e
                                );
                            }
                        }
                    }
                }
                Err(hr) => {
                    log::warn!("call to initialize_current_thread failed: {}", hr);
                }
            }
        });

        Self {
            sender,
            handle,
            modules: HashMap::new(),
            modules_mutex: Mutex::new(()),
        }
    }

    pub fn shutdown(self) {
        // dropping channel sender causes the channel receiver to Err and break out thread loop.
        drop(self.sender);
        match self.handle.join() {
            Ok(()) => log::trace!("rejit thread finished"),
            Err(err) => log::error!("Error in joining rejit thread"),
        }
    }

    pub fn enqueue_for_rejit(&self, module_ids: Vec<ModuleID>, method_ids: Vec<mdMethodDef>) {
        if let Err(err) = self.sender.send(RejitItem {
            module_ids,
            method_ids,
        }) {
            log::warn!(
                "Unable to send module_ids={:?} method_ids={:?} for rejit",
                &err.0.method_ids,
                &err.0.module_ids
            );
        }
    }

    pub fn notify_rejit_compilation_started(
        &self,
        function_id: FunctionID,
        rejit_id: ReJITID,
    ) -> Result<(), HRESULT> {
        log::debug!(
            "notify_rejit_compilation_started: function_id={}, rejit_id={}",
            function_id,
            rejit_id
        );
        Ok(())
    }

    pub fn notify_rejit_parameters(
        &mut self,
        module_id: ModuleID,
        method_id: mdMethodDef,
        function_control: &ICorProfilerFunctionControl,
        module_metadata: &ModuleMetadata,
        module_wrapper_tokens: &mut ModuleWrapperTokens,
        profiler_info: &ICorProfilerInfo4,
        call_target_tokens: &mut CallTargetTokens,
    ) -> Result<(), HRESULT> {
        log::debug!(
            "notify_rejit_parameters: module_id={} method_id={}, {}",
            module_id,
            method_id,
            &module_metadata.assembly_name
        );

        let rejit_module = self.get_or_add_module(module_id);
        let rejit_method = rejit_module.get_or_add_method(method_id);

        if rejit_method.function_info().is_none() {
            log::warn!(
                "notify_rejit_parameters: function_info is missing for method_def={}",
                method_id
            );
            return Err(S_FALSE);
        }

        if rejit_method.method_replacement.is_none() {
            log::warn!(
                "notify_rejit_parameters: method_replacement is missing for method_def={}",
                method_id
            );
            return Err(S_FALSE);
        }

        calltarget_rewriter_callback(
            module_metadata,
            module_wrapper_tokens,
            rejit_module,
            method_id,
            function_control,
            profiler_info,
            call_target_tokens,
        )
    }

    pub fn get_or_add_module(&mut self, module_id: ModuleID) -> &mut RejitHandlerModule {
        let _lock = self.modules_mutex.lock().unwrap();
        self.modules
            .entry(module_id)
            .or_insert_with(|| RejitHandlerModule::new(module_id))
    }
}

pub fn calltarget_rewriter_callback(
    module_metadata: &ModuleMetadata,
    module_wrapper_tokens: &mut ModuleWrapperTokens,
    rejit_handler_module: &mut RejitHandlerModule,
    method_id: mdMethodDef,
    function_control: &ICorProfilerFunctionControl,
    profiler_info: &ICorProfilerInfo4,
    call_target_tokens: &mut CallTargetTokens,
) -> Result<(), HRESULT> {
    log::trace!("called calltarget_rewriter_callback");

    let module_id = rejit_handler_module.module_id;
    let rejit_handler_module_method = rejit_handler_module.get_or_add_method(method_id);

    let caller = rejit_handler_module_method.function_info.as_ref().unwrap();
    let function_token = caller.id;
    let parsed_function_method_signature = caller.method_signature.try_parse().unwrap();
    let ret_func_arg = parsed_function_method_signature.return_type();
    let method_replacement = rejit_handler_module_method
        .method_replacement
        .as_ref()
        .unwrap();

    let (_, ret_type_flags) = ret_func_arg.get_type_flags();
    let is_void = ret_type_flags.contains(MethodArgumentTypeFlag::VOID);
    let is_static = !caller
        .method_signature
        .calling_convention()
        .unwrap()
        .contains(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_HASTHIS);

    let method_arguments = parsed_function_method_signature.arguments();
    let num_args = parsed_function_method_signature.arg_len;
    let wrapper = method_replacement.wrapper().unwrap();
    let wrapper_method_key = wrapper.get_method_cache_key();

    let wrapper_method_ref = process::get_wrapper_method_ref(
        profiler_info,
        module_metadata,
        module_wrapper_tokens,
        module_id,
        wrapper,
        &wrapper_method_key,
    )?;

    let meta_emit = &module_metadata.emit;
    let meta_import = &module_metadata.import;

    log::debug!("calltarget_rewriter_callback: start {}() [is_void={}, is_static={}, integration_type={}, arguments={}]",
        caller.full_name(),
        is_void,
        is_static,
        &wrapper.type_name,
        num_args
    );

    if !crate::profiler::profiler_assembly_loaded_in_app_domain(module_metadata.app_domain_id) {
        log::warn!("calltarget_rewriter_callback: skipping method as method replacement \
            found but profiler has not been loaded into app domain. app_domain_id={}, token={}, caller_name={}()",
            module_metadata.app_domain_id,
            function_token,
            caller.full_name()
        );
        return Err(S_FALSE);
    }

    let il_body = profiler_info.get_il_function_body(module_id, function_token)?;
    let mut method = Method::new(il_body.into()).map_err(|e| {
        log::warn!("calltarget_rewriter_callback: error decoding il. {:?}", e);
        S_FALSE
    })?;

    let original_il = if *env::ELASTIC_APM_PROFILER_LOG_IL {
        Some(helpers::get_il_codes(
            "IL original code for caller: ",
            &method,
            caller,
            module_metadata,
        ))
    } else {
        None
    };

    // expand to a fat method with fat sections to work with
    method.expand_tiny_to_fat();
    method.expand_small_sections_to_fat();

    let (local_sig, instructions) =
        call_target_tokens.modify_local_sig_and_initialize(&method, caller, module_metadata)?;

    method
        .header
        .set_local_var_sig_tok(local_sig.new_local_var_sig);

    let mut idx: usize = instructions.len();
    method.insert_prelude(instructions).map_err(|_| S_FALSE)?;

    let type_info = caller.type_info.as_ref().unwrap();

    if is_static {
        if type_info.is_value_type {
            log::warn!("calltarget_rewriter_callback: static methods on value types cannot be instrumented");
            return Err(S_FALSE);
        }
        method
            .insert(idx, Instruction::ldnull())
            .map_err(|_| S_FALSE)?;
        idx += 1;
    } else {
        method
            .insert(idx, Instruction::ldarg_0())
            .map_err(|_| S_FALSE)?;
        idx += 1;

        if type_info.is_value_type {
            if type_info.type_spec != mdTypeSpecNil {
                method
                    .insert(idx, Instruction::ldobj(type_info.type_spec))
                    .map_err(|_| S_FALSE)?;
                idx += 1;
            } else if !type_info.is_generic {
                method
                    .insert(idx, Instruction::ldobj(type_info.id))
                    .map_err(|_| S_FALSE)?;
                idx += 1;
            } else {
                // Generic struct instrumentation is not supported
                // IMetaDataImport::GetMemberProps and IMetaDataImport::GetMemberRefProps returns
                // The parent token as mdTypeDef and not as a mdTypeSpec
                // that's because the method definition is stored in the mdTypeDef
                // The problem is that we don't have the exact Spec of that generic
                // We can't emit LoadObj or Box because that would result in an invalid IL.
                // This problem doesn't occur on a class type because we can always relay in the
                // object type.
                return Err(S_FALSE);
            }
        }
    }

    // insert instructions for loading arguments
    if num_args < CallTargetTokens::FAST_PATH_COUNT as u8 {
        // load arguments directly
        for (i, method_argument) in method_arguments.iter().enumerate() {
            let arg = if is_static { i } else { i + 1 };
            method
                .insert(idx, Instruction::load_argument(arg as u16))
                .map_err(|_| S_FALSE)?;
            idx += 1;
            let (_, flags) = method_argument.get_type_flags();
            if flags.contains(MethodArgumentTypeFlag::BY_REF) {
                log::warn!("calltarget_rewriter_callback: methods with ref parameters cannot be instrumented");
                return Err(S_FALSE);
            }
        }
    } else {
        // load into an object array
        method
            .insert(idx, Instruction::load_int32(num_args as i32))
            .map_err(|_| S_FALSE)?;
        idx += 1;
        method
            .insert(
                idx,
                Instruction::newarr(call_target_tokens.get_object_type_ref()),
            )
            .map_err(|_| S_FALSE)?;
        idx += 1;
        for (i, method_argument) in method_arguments.iter().enumerate() {
            method
                .insert(idx, Instruction::dup())
                .map_err(|_| S_FALSE)?;
            idx += 1;
            method
                .insert(idx, Instruction::load_int32(i as i32))
                .map_err(|_| S_FALSE)?;
            idx += 1;

            let arg = if is_static { i } else { i + 1 };
            method
                .insert(idx, Instruction::load_argument(arg as u16))
                .map_err(|_| S_FALSE)?;
            idx += 1;

            let (_, flags) = method_argument.get_type_flags();
            if flags.contains(MethodArgumentTypeFlag::BY_REF) {
                log::warn!("calltarget_rewriter_callback: methods with ref parameters cannot be instrumented");
                return Err(S_FALSE);
            }

            if flags.contains(MethodArgumentTypeFlag::BOXED_TYPE) {
                let tok = method_argument
                    .get_type_tok(meta_emit, call_target_tokens.get_cor_lib_assembly_ref())?;
                if tok == mdTokenNil {
                    return Err(S_FALSE);
                }
                method
                    .insert(idx, Instruction::box_(tok))
                    .map_err(|_| S_FALSE)?;
                idx += 1;
            }

            method
                .insert(idx, Instruction::stelem_ref())
                .map_err(|_| S_FALSE)?;
            idx += 1;
        }
    }

    if log::log_enabled!(Level::Debug) {
        log_caller_type_info(caller, type_info);
    }

    let begin_method = call_target_tokens.write_begin_method(
        wrapper_method_ref.type_ref,
        type_info,
        &method_arguments,
        module_metadata,
    )?;

    method.insert(idx, begin_method).map_err(|_| S_FALSE)?;
    idx += 1;

    method
        .insert(
            idx,
            Instruction::store_local(local_sig.call_target_state_index as u16),
        )
        .map_err(|_| S_FALSE)?;
    idx += 1;

    // Capture the idx so that we can update the instruction offset later
    method
        .insert(idx, Instruction::leave_s(-1))
        .map_err(|_| S_FALSE)?;
    let state_leave_to_begin_original_method_idx = idx;
    idx += 1;

    let log_exception = call_target_tokens.write_log_exception(
        wrapper_method_ref.type_ref,
        type_info,
        module_metadata,
    )?;

    // clone the log exception instruction as we'll use the original later
    method
        .insert(idx, log_exception.clone())
        .map_err(|_| S_FALSE)?;
    let begin_method_log_exception_idx = idx;
    idx += 1;

    method
        .insert(idx, Instruction::leave_s(0))
        .map_err(|_| S_FALSE)?;
    let begin_method_catch_leave_idx = idx;
    idx += 1;

    let offsets = method.get_instruction_offsets();

    let begin_method_ex_clause = FatSectionClause {
        flag: CorExceptionFlag::COR_ILEXCEPTION_CLAUSE_NONE,
        try_offset: 0,
        try_length: offsets[begin_method_log_exception_idx] as u32,
        handler_offset: offsets[begin_method_log_exception_idx] as u32,
        handler_length: (offsets[begin_method_catch_leave_idx + 1]
            - offsets[begin_method_log_exception_idx]) as u32,
        class_token_or_filter_offset: call_target_tokens.get_ex_type_ref(),
    };

    // original method

    // The idx of the start of the original instructions
    let begin_original_method_idx = idx;

    // update the leave_s instruction offset to point to the original method
    if let Some(state_leave) = method
        .instructions
        .get_mut(state_leave_to_begin_original_method_idx)
    {
        if let ShortInlineBrTarget(offset) = &mut state_leave.operand {
            let state_offset = (offsets[begin_original_method_idx]
                - offsets[state_leave_to_begin_original_method_idx + 1])
                as u32;
            *offset = state_offset as i8;
        }
    }

    // write end

    // add return instruction at the end
    idx = method.instructions.len();
    method
        .insert(idx, Instruction::ret())
        .map_err(|_| S_FALSE)?;

    // store any original exception that might be thrown, so that we can capture it in our end method
    method
        .insert(
            idx,
            Instruction::store_local(local_sig.exception_index as u16),
        )
        .map_err(|_| S_FALSE)?;
    let mut start_exception_catch_idx = idx;
    idx += 1;

    // then rethrow any original exception
    method
        .insert(idx, Instruction::rethrow())
        .map_err(|_| S_FALSE)?;
    let mut rethrow_idx = idx;
    idx += 1;

    let mut end_method_try_start_idx = idx;
    if is_static {
        if type_info.is_value_type {
            log::warn!("calltarget_rewriter_callback: static methods on value types cannot be instrumented");
            return Err(S_FALSE);
        }
        method
            .insert(idx, Instruction::ldnull())
            .map_err(|_| S_FALSE)?;
        idx += 1;
    } else {
        method
            .insert(idx, Instruction::ldarg_0())
            .map_err(|_| S_FALSE)?;
        idx += 1;

        if type_info.is_value_type {
            if type_info.type_spec != mdTypeSpecNil {
                method
                    .insert(idx, Instruction::ldobj(type_info.type_spec))
                    .map_err(|_| S_FALSE)?;
                idx += 1;
            } else if !type_info.is_generic {
                method
                    .insert(idx, Instruction::ldobj(type_info.id))
                    .map_err(|_| S_FALSE)?;
                idx += 1;
            } else {
                // Generic struct instrumentation is not supported
                // IMetaDataImport::GetMemberProps and IMetaDataImport::GetMemberRefProps returns
                // The parent token as mdTypeDef and not as a mdTypeSpec
                // that's because the method definition is stored in the mdTypeDef
                // The problem is that we don't have the exact Spec of that generic
                // We can't emit LoadObj or Box because that would result in an invalid IL.
                // This problem doesn't occur on a class type because we can always relay in the
                // object type.
                return Err(S_FALSE);
            }
        }
    }

    if !is_void {
        method
            .insert(
                idx,
                Instruction::load_local(local_sig.return_value_index as u16),
            )
            .map_err(|_| S_FALSE)?;
        idx += 1;
    }

    method
        .insert(
            idx,
            Instruction::load_local(local_sig.exception_index as u16),
        )
        .map_err(|_| S_FALSE)?;
    idx += 1;
    method
        .insert(
            idx,
            Instruction::load_local(local_sig.call_target_state_index as u16),
        )
        .map_err(|_| S_FALSE)?;
    idx += 1;

    let end_method_call_instruction = if is_void {
        call_target_tokens.write_end_void_return_member_ref(
            wrapper_method_ref.type_ref,
            type_info,
            module_metadata,
        )?
    } else {
        call_target_tokens.write_end_return_member_ref(
            wrapper_method_ref.type_ref,
            type_info,
            &ret_func_arg,
            module_metadata,
        )?
    };

    method
        .insert(idx, end_method_call_instruction)
        .map_err(|_| S_FALSE)?;
    idx += 1;

    method
        .insert(
            idx,
            Instruction::store_local(local_sig.call_target_return_index as u16),
        )
        .map_err(|_| S_FALSE)?;
    idx += 1;

    if !is_void {
        method
            .insert(
                idx,
                Instruction::load_local_address(local_sig.call_target_return_index as u16),
            )
            .map_err(|_| S_FALSE)?;
        idx += 1;

        let get_return_value_instruction = call_target_tokens
            .write_call_target_return_get_return_value(
                local_sig.call_target_return_token,
                module_metadata,
            )?;

        method
            .insert(idx, get_return_value_instruction)
            .map_err(|_| S_FALSE)?;
        idx += 1;
        method
            .insert(
                idx,
                Instruction::store_local(local_sig.return_value_index as u16),
            )
            .map_err(|_| S_FALSE)?;
        idx += 1;
    }

    method
        .insert(idx, Instruction::leave_s(-1))
        .map_err(|_| S_FALSE)?;
    let mut end_method_try_leave_idx = idx;
    idx += 1;

    method.insert(idx, log_exception).map_err(|_| S_FALSE)?;
    let mut end_method_catch_start_idx = idx;
    idx += 1;

    method
        .insert(idx, Instruction::leave_s(0))
        .map_err(|_| S_FALSE)?;
    let mut end_method_catch_leave_idx = idx;
    idx += 1;

    method
        .insert(idx, Instruction::endfinally())
        .map_err(|_| S_FALSE)?;
    let mut end_finally_idx = idx;
    idx += 1;

    if !is_void {
        method
            .insert(
                idx,
                Instruction::load_local(local_sig.return_value_index as u16),
            )
            .map_err(|_| S_FALSE)?;
    }

    let mut i = start_exception_catch_idx;
    let mut added_instruction_count = 0;

    // change all original method ret instructions to leave.s or leave instructions
    // with an offset pointing to the instruction before the ending ret instruction.
    while i > begin_original_method_idx {
        if method.instructions[i].opcode != RET {
            i -= 1;
            continue;
        }

        let mut current = i;

        if !is_void {
            // Since we're adding additional instructions to the original method,
            // make a note of how many are added so that we can later increment the indices
            // for instructions that are targets for clauses that come after the original
            // method instructions
            added_instruction_count += 1;
            method
                .insert(
                    i,
                    Instruction::store_local(local_sig.return_value_index as u16),
                )
                .map_err(|_| S_FALSE)?;

            i -= 1;
            current += 1;
        }

        // calculate the offset to the target instruction to determine whether to
        // insert a leave_s or leave instruction
        let leave_instr = {
            let mut leave_offset = method
                .instructions
                .iter()
                .skip(current + 1)
                .map(|i| i.len())
                .sum::<usize>()
                - Instruction::ret().len();

            if !is_void {
                leave_offset -= Instruction::load_local(local_sig.return_value_index as u16).len();
            }

            if leave_offset > i8::MAX as usize {
                Instruction::leave(leave_offset as i32)
            } else {
                Instruction::leave_s(leave_offset as i8)
            }
        };

        method.replace(current, leave_instr).map_err(|_| S_FALSE)?;
        i -= 1;
    }

    if added_instruction_count > 0 {
        start_exception_catch_idx += added_instruction_count;
        rethrow_idx += added_instruction_count;
        end_method_try_start_idx += added_instruction_count;
        end_method_try_leave_idx += added_instruction_count;
        end_method_catch_start_idx += added_instruction_count;
        end_method_catch_leave_idx += added_instruction_count;
        end_finally_idx += added_instruction_count;
    }

    let offsets = method.get_instruction_offsets();

    // update the end method leave_s instruction offset to point to the endfinally
    if let Some(end_method_try_leave) = method.instructions.get_mut(end_method_try_leave_idx) {
        if let ShortInlineBrTarget(offset) = &mut end_method_try_leave.operand {
            let finally_offset =
                (offsets[end_finally_idx] - offsets[end_method_try_leave_idx + 1]) as u32;
            *offset = finally_offset as i8;
        }
    }

    // create all the later clauses, now that all instructions are inserted
    let end_method_ex_clause = {
        FatSectionClause {
            flag: CorExceptionFlag::COR_ILEXCEPTION_CLAUSE_NONE,
            try_offset: offsets[end_method_try_start_idx],
            try_length: offsets[end_method_try_leave_idx + 1] - offsets[end_method_try_start_idx],
            handler_offset: offsets[end_method_catch_start_idx],
            handler_length: offsets[end_method_catch_leave_idx + 1]
                - offsets[end_method_catch_start_idx],
            class_token_or_filter_offset: call_target_tokens.get_ex_type_ref(),
        }
    };

    let ex_clause = FatSectionClause {
        flag: CorExceptionFlag::COR_ILEXCEPTION_CLAUSE_NONE,
        try_offset: 0,
        try_length: offsets[start_exception_catch_idx],
        handler_offset: offsets[start_exception_catch_idx],
        handler_length: offsets[rethrow_idx + 1] - offsets[start_exception_catch_idx],
        class_token_or_filter_offset: call_target_tokens.get_ex_type_ref(),
    };

    let finally_clause = FatSectionClause {
        flag: CorExceptionFlag::COR_ILEXCEPTION_CLAUSE_FINALLY,
        try_offset: 0,
        try_length: offsets[rethrow_idx + 1],
        handler_offset: offsets[rethrow_idx + 1],
        handler_length: offsets[end_finally_idx + 1] - offsets[rethrow_idx + 1],
        class_token_or_filter_offset: mdTokenNil,
    };

    // add the exception handling clauses to the method
    method
        .push_clauses(vec![
            begin_method_ex_clause,
            end_method_ex_clause,
            ex_clause,
            finally_clause,
        ])
        .map_err(|e| {
            log::warn!("calltarget_rewriter_callback: could not add clauses to method");
            S_FALSE
        })?;

    if *env::ELASTIC_APM_PROFILER_LOG_IL {
        let modified_il = helpers::get_il_codes(
            "IL modification for caller: ",
            &method,
            caller,
            module_metadata,
        );
        log::debug!("{}\n{}", original_il.unwrap_or_default(), modified_il);
    }

    let method_bytes = method.into_bytes();

    // write the new IL
    function_control
        .set_il_function_body(&method_bytes)
        .map_err(|e| {
            log::warn!(
                "calltarget_rewriter_callback: failed to set il function body for \
            module_id={} function_token={}",
                module_id,
                function_token
            );
            e
        })?;

    log::info!("calltarget_rewriter_callback: finished {}() [is_void={}, is_static={}, integration_type={}, arguments={}]",
        caller.full_name(),
        is_void,
        is_static,
        &wrapper.type_name,
        num_args
    );

    Ok(())
}

fn log_caller_type_info(caller: &FunctionInfo, type_info: &TypeInfo) {
    let mut s = vec![
        format!("caller type.id: {}", caller.id),
        format!("caller type.is_generic: {}", type_info.is_generic),
        format!("caller type.name: {}", &type_info.name),
        format!("caller type.token_type: {:?}", type_info.token_type),
        format!("caller type.spec: {}", type_info.type_spec),
        format!("caller type.is_value_type: {}", type_info.is_value_type),
    ];

    if let Some(extend_from) = &type_info.extends_from {
        s.push(format!("caller type extend_from.id: {}", extend_from.id));
        s.push(format!(
            "caller type extend_from.is_generic: {}",
            extend_from.is_generic
        ));
        s.push(format!(
            "caller type extend_from.name: {}",
            &extend_from.name
        ));
        s.push(format!(
            "caller type extend_from.token_type: {:?}",
            extend_from.token_type
        ));
        s.push(format!(
            "caller type extend_from.spec: {}",
            extend_from.type_spec
        ));
        s.push(format!(
            "caller type extend_from.is_value_type: {}",
            extend_from.is_value_type
        ));
    }

    if let Some(parent_type) = &type_info.parent_type {
        s.push(format!("caller parent_type.id: {}", parent_type.id));
        s.push(format!(
            "caller parent_type.is_generic: {}",
            parent_type.is_generic
        ));
        s.push(format!("caller parent_type.name: {}", &parent_type.name));
        s.push(format!(
            "caller parent_type.token_type: {:?}",
            parent_type.token_type
        ));
        s.push(format!(
            "caller parent_type.spec: {}",
            parent_type.type_spec
        ));
        s.push(format!(
            "caller parent_type.is_value_type: {}",
            parent_type.is_value_type
        ));
    }

    log::debug!("{}", s.join("\n"));
}
