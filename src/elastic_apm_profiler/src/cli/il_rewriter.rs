use crate::{
    cli::{ExceptionHandlingClauseFlags, Instruction, Method, MethodHeaderFlags, Section},
    ffi::{mdToken, mdTokenNil, ModuleID, BYTE, E_FAIL},
    interfaces::{icor_profiler_info::ICorProfilerInfo, imethod_malloc::IMethodMalloc},
};
use com::sys::HRESULT;

struct IlRewriter {
    profiler_info: ICorProfilerInfo,
    module_id: ModuleID,
    method_token: mdToken,
    generate_tiny_header: bool,
    tk_local_var_sig: mdToken,
    eh_clause: Vec<Section>,
    offset_to_instr: Vec<Instruction>,
    output_buffer: Vec<BYTE>,
    method_malloc: Option<IMethodMalloc>,
    max_stack: u32,
    flags: MethodHeaderFlags,
    code_size: u32,
}

struct EHClause {
    flags: ExceptionHandlingClauseFlags,
}

impl IlRewriter {
    pub fn new_with_profiler_info(
        profiler_info: ICorProfilerInfo,
        module_id: ModuleID,
        method_token: mdToken,
    ) -> Self {
        Self {
            profiler_info,
            module_id,
            method_token,
            generate_tiny_header: false,
            tk_local_var_sig: mdTokenNil,
            eh_clause: vec![],
            offset_to_instr: vec![],
            output_buffer: vec![],
            method_malloc: None,
            max_stack: 0,
            flags: MethodHeaderFlags::empty(),
            code_size: 0,
        }
    }

    pub fn initialize_tiny(&mut self) {
        self.tk_local_var_sig = mdTokenNil;
        self.max_stack = 8;
        self.flags = MethodHeaderFlags::CorILMethod_TinyFormat;
        self.code_size = 0;
        self.generate_tiny_header = true;
    }

    pub fn get_tk_local_var_sig(&self) -> mdToken {
        self.tk_local_var_sig
    }

    pub fn set_tk_local_var_sig(&mut self, local_var_sig: mdToken) {
        self.tk_local_var_sig = local_var_sig;
        self.generate_tiny_header = false;
    }

    pub fn import(&mut self) -> Result<(), HRESULT> {
        let il_body = self
            .profiler_info
            .get_il_function_body(self.module_id, self.method_token)?;
        let method = Method::new(il_body.method_header, il_body.method_size).map_err(|e| E_FAIL)?;

        self.import_il(method.instructions);
        self.import_eh(method.sections);
        Ok(())
    }

    fn import_il(&mut self, instructions: Vec<Instruction>) {
        self.offset_to_instr = instructions;
    }

    fn import_eh(&mut self, sections: Vec<Section>) {
        self.eh_clause = sections;
    }
}
