// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::{
    cil::{compress_token, FatMethodHeader, Instruction, Method, MethodHeader, CorILMethodFlags},
    ffi::{
        mdMethodDef, mdToken, CorCallingConvention, CorElementType, CorFieldAttr, CorMethodAttr,
        CorMethodImpl, CorPinvokeMap, CorTypeAttr, ModuleID, COR_SIGNATURE, E_FAIL, ULONG,
    },
    interfaces::ICorProfilerInfo4,
    profiler::{env, helpers, managed, types::ModuleMetadata},
};
use com::sys::HRESULT;

pub fn run_il_startup_hook(
    profiler_info: &ICorProfilerInfo4,
    module_metadata: &ModuleMetadata,
    module_id: ModuleID,
    function_token: mdToken,
) -> Result<(), HRESULT> {
    let startup_method_def =
        generate_void_il_startup_method(profiler_info, module_id, module_metadata)?;
    let il_body = profiler_info.get_il_function_body(module_id, function_token)?;
    let mut method = Method::new(il_body.into()).map_err(|e| {
        log::warn!("run_il_startup_hook: error decoding il. {:?}", e);
        E_FAIL
    })?;

    method
        .insert_prelude(vec![Instruction::call(startup_method_def)])
        .map_err(|e| {
            log::warn!("run_il_startup_hook: error inserting prelude. {:?}", e);
            E_FAIL
        })?;

    let method_bytes = method.into_bytes();
    let allocator = profiler_info.get_il_function_body_allocator(module_id)?;
    let allocated_bytes = allocator.alloc(method_bytes.len() as ULONG)?;
    let address = unsafe { allocated_bytes.into_inner() };
    unsafe {
        std::ptr::copy(method_bytes.as_ptr(), address, method_bytes.len());
    }
    profiler_info
        .set_il_function_body(module_id, function_token, address as *const _)
        .map_err(|e| {
            log::warn!("run_il_startup_hook: failed to set il for startup hook");
            e
        })?;

    Ok(())
}

fn generate_void_il_startup_method(
    profiler_info: &ICorProfilerInfo4,
    module_id: ModuleID,
    module_metadata: &ModuleMetadata,
) -> Result<mdMethodDef, HRESULT> {
    let mscorlib_ref = helpers::create_assembly_ref_to_mscorlib(&module_metadata.assembly_emit)?;

    log::trace!(
        "generate_void_il_startup_method: created mscorlib ref {}",
        mscorlib_ref
    );

    let object_type_ref = module_metadata
        .emit
        .define_type_ref_by_name(mscorlib_ref, "System.Object")
        .map_err(|e| {
            log::warn!("error defining type ref by name for System.Object. {:X}", e);
            e
        })?;

    let new_type_def = module_metadata
        .emit
        .define_type_def(
            "__ElasticVoidMethodType__",
            CorTypeAttr::tdAbstract | CorTypeAttr::tdSealed,
            object_type_ref,
            None,
        )
        .map_err(|e| {
            log::warn!("error defining type def __ElasticVoidMethodType__. {:X}", e);
            e
        })?;

    let initialize_signature = &[
        CorCallingConvention::IMAGE_CEE_CS_CALLCONV_DEFAULT.bits(),
        0,
        CorElementType::ELEMENT_TYPE_VOID as COR_SIGNATURE,
    ];

    log::trace!("generate_void_il_startup_method: define method __ElasticVoidMethodCall__");

    let new_method = module_metadata
        .emit
        .define_method(
            new_type_def,
            "__ElasticVoidMethodCall__",
            CorMethodAttr::mdStatic,
            initialize_signature,
            0,
            CorMethodImpl::miIL,
        )
        .map_err(|e| {
            log::warn!("error defining method __ElasticVoidMethodCall__. {:X}", e);
            e
        })?;

    let field_signature = &[
        CorCallingConvention::IMAGE_CEE_CS_CALLCONV_FIELD.bits(),
        CorElementType::ELEMENT_TYPE_I4 as COR_SIGNATURE,
    ];

    let is_assembly_loaded_field_def = module_metadata
        .emit
        .define_field(
            new_type_def,
            "_isAssemblyLoaded",
            CorFieldAttr::fdStatic | CorFieldAttr::fdPrivate,
            field_signature,
            CorElementType::ELEMENT_TYPE_END,
            None,
            0,
        )
        .map_err(|e| {
            log::warn!("error defining field _isAssemblyLoaded. {:X}", e);
            e
        })?;

    let already_loaded_signature = &[
        CorCallingConvention::IMAGE_CEE_CS_CALLCONV_DEFAULT.bits(),
        0,
        CorElementType::ELEMENT_TYPE_BOOLEAN as COR_SIGNATURE,
    ];

    let already_loaded_method_token = module_metadata
        .emit
        .define_method(
            new_type_def,
            "IsAlreadyLoaded",
            CorMethodAttr::mdStatic | CorMethodAttr::mdPrivate,
            already_loaded_signature,
            0,
            CorMethodImpl::miIL,
        )
        .map_err(|e| {
            log::warn!("error defining method IsAlreadyLoaded. {:X}", e);
            e
        })?;

    let interlocked_type_ref = module_metadata
        .emit
        .define_type_ref_by_name(mscorlib_ref, "System.Threading.Interlocked")
        .map_err(|e| {
            log::warn!(
                "error defining type ref by name for System.Threading.Interlocked. {:X}",
                e
            );
            e
        })?;

    // Create method signature for System.Threading.Interlocked::CompareExchange(int32&, int32, int32)
    let interlocked_compare_exchange_signature = &[
        CorCallingConvention::IMAGE_CEE_CS_CALLCONV_DEFAULT.bits(),
        3,
        CorElementType::ELEMENT_TYPE_I4 as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_BYREF as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_I4 as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_I4 as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_I4 as COR_SIGNATURE,
    ];

    let interlocked_compare_member_ref = module_metadata
        .emit
        .define_member_ref(
            interlocked_type_ref,
            "CompareExchange",
            interlocked_compare_exchange_signature,
        )
        .map_err(|e| {
            log::warn!("error defining member ref CompareExchange. {:X}", e);
            e
        })?;

    // Write the instructions for the IsAlreadyLoaded method
    let instructions = vec![
        Instruction::ldsflda(is_assembly_loaded_field_def),
        Instruction::ldc_i4_1(),
        Instruction::ldc_i4_0(),
        Instruction::call(interlocked_compare_member_ref),
        Instruction::ldc_i4_1(),
        Instruction::ceq(),
        Instruction::ret(),
    ];

    let method_bytes = Method::tiny(instructions)
        .map_err(|e| {
            log::warn!("failed to define IsAlreadyLoaded method");
            E_FAIL
        })?
        .into_bytes();

    let allocator = profiler_info.get_il_function_body_allocator(module_id)?;
    let allocated_bytes = allocator.alloc(method_bytes.len() as ULONG)?;
    let address = unsafe { allocated_bytes.into_inner() };
    unsafe {
        std::ptr::copy(method_bytes.as_ptr(), address, method_bytes.len());
    }
    log::trace!("generate_void_il_startup_method: write IsAlreadyLoaded body");
    profiler_info
        .set_il_function_body(module_id, already_loaded_method_token, address as *const _)
        .map_err(|e| {
            log::warn!("generate_void_il_startup_method: failed to set il for IsAlreadyLoaded");
            e
        })?;

    let get_assembly_bytes_signature = &[
        CorCallingConvention::IMAGE_CEE_CS_CALLCONV_DEFAULT.bits(),
        4,
        CorElementType::ELEMENT_TYPE_VOID as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_BYREF as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_I as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_BYREF as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_I4 as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_BYREF as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_I as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_BYREF as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_I4 as COR_SIGNATURE,
    ];

    let pinvoke_method_def = module_metadata.emit.define_method(
        new_type_def,
        "GetAssemblyAndSymbolsBytes",
        CorMethodAttr::mdStatic | CorMethodAttr::mdPinvokeImpl | CorMethodAttr::mdHideBySig,
        get_assembly_bytes_signature,
        0,
        CorMethodImpl::empty()).map_err(|e| {
        log::warn!("generate_void_il_startup_method: failed to define method GetAssemblyAndSymbolsBytes");
        e
    })?;

    module_metadata.emit.set_method_impl_flags(pinvoke_method_def, CorMethodImpl::miPreserveSig).map_err(|e| {
        log::warn!("generate_void_il_startup_method: failed to set method impl flags for GetAssemblyAndSymbolsBytes");
        e
    })?;

    let native_profiler_file = env::get_native_profiler_file()?;
    let profiler_ref = module_metadata
        .emit
        .define_module_ref(&native_profiler_file)?;

    module_metadata.emit.define_pinvoke_map(
        pinvoke_method_def,
        CorPinvokeMap::empty(),
        "GetAssemblyAndSymbolsBytes",
        profiler_ref).map_err(|e| {
        log::warn!("generate_void_il_startup_method: failed to define pinvoke map for GetAssemblyAndSymbolsBytes");
        e
    })?;

    let byte_type_ref = module_metadata.emit.define_type_ref_by_name(mscorlib_ref, "System.Byte").map_err(|e| {
        log::warn!("generate_void_il_startup_method: failed to define type ref by name for System.Byte");
        e
    })?;
    let marshal_type_ref = module_metadata.emit.define_type_ref_by_name(mscorlib_ref, "System.Runtime.InteropServices.Marshal").map_err(|e| {
        log::warn!("generate_void_il_startup_method: failed to define type ref by name for System.Runtime.InteropServices.Marshal");
        e
    })?;

    let marshal_copy_signature = &[
        CorCallingConvention::IMAGE_CEE_CS_CALLCONV_DEFAULT.bits(),
        4,
        CorElementType::ELEMENT_TYPE_VOID as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_I as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_SZARRAY as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_U1 as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_I4 as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_I4 as COR_SIGNATURE,
    ];

    let marshal_copy_member_ref = module_metadata
        .emit
        .define_member_ref(marshal_type_ref, "Copy", marshal_copy_signature)
        .map_err(|e| {
            log::warn!("generate_void_il_startup_method: failed to define member ref for Copy");
            e
        })?;

    let system_reflection_assembly_type_ref = module_metadata.emit.define_type_ref_by_name(mscorlib_ref, "System.Reflection.Assembly").map_err(|e| {
        log::warn!("generate_void_il_startup_method: failed to define type ref by name for System.Reflection.Assembly");
        e
    })?;

    let system_appdomain_type_ref = module_metadata.emit.define_type_ref_by_name(mscorlib_ref, "System.AppDomain").map_err(|e| {
        log::warn!("generate_void_il_startup_method: failed to define type ref by name for System.AppDomain");
        e
    })?;

    let mut appdomain_get_current_domain_signature: Vec<COR_SIGNATURE> = vec![
        CorCallingConvention::IMAGE_CEE_CS_CALLCONV_DEFAULT.bits(),
        0,
        CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE,
    ];
    appdomain_get_current_domain_signature
        .append(&mut compress_token(system_appdomain_type_ref).unwrap());

    let appdomain_get_current_domain_member_ref = module_metadata
        .emit
        .define_member_ref(
            system_appdomain_type_ref,
            "get_CurrentDomain",
            &appdomain_get_current_domain_signature,
        )
        .map_err(|e| {
            log::warn!(
                "generate_void_il_startup_method: failed to define member ref get_CurrentDomain"
            );
            e
        })?;

    let mut appdomain_load_signature = vec![
        CorCallingConvention::IMAGE_CEE_CS_CALLCONV_HASTHIS.bits(),
        2,
        CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE,
    ];
    appdomain_load_signature
        .append(&mut compress_token(system_reflection_assembly_type_ref).unwrap());
    appdomain_load_signature.push(CorElementType::ELEMENT_TYPE_SZARRAY as COR_SIGNATURE);
    appdomain_load_signature.push(CorElementType::ELEMENT_TYPE_U1 as COR_SIGNATURE);
    appdomain_load_signature.push(CorElementType::ELEMENT_TYPE_SZARRAY as COR_SIGNATURE);
    appdomain_load_signature.push(CorElementType::ELEMENT_TYPE_U1 as COR_SIGNATURE);

    let appdomain_load_member_ref = module_metadata
        .emit
        .define_member_ref(system_appdomain_type_ref, "Load", &appdomain_load_signature)
        .map_err(|e| {
            log::warn!("generate_void_il_startup_method: failed to define member ref Load");
            e
        })?;

    let assembly_create_instance_signature = &[
        CorCallingConvention::IMAGE_CEE_CS_CALLCONV_HASTHIS.bits(),
        1,
        CorElementType::ELEMENT_TYPE_OBJECT as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_STRING as COR_SIGNATURE,
    ];

    let assembly_create_instance_member_ref = module_metadata
        .emit
        .define_member_ref(
            system_reflection_assembly_type_ref,
            "CreateInstance",
            assembly_create_instance_signature,
        )
        .map_err(|e| {
            log::warn!(
                "generate_void_il_startup_method: failed to define member ref CreateInstance"
            );
            e
        })?;

    let load_helper_token = module_metadata
        .emit
        .define_user_string(managed::MANAGED_PROFILER_ASSEMBLY_LOADER_STARTUP)
        .map_err(|e| {
            log::warn!(
                "generate_void_il_startup_method: failed to define user string {}",
                managed::MANAGED_PROFILER_ASSEMBLY_LOADER_STARTUP
            );
            e
        })?;

    let mut locals_signature = vec![
        CorCallingConvention::IMAGE_CEE_CS_CALLCONV_LOCAL_SIG.bits(),
        7,
        CorElementType::ELEMENT_TYPE_I as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_I4 as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_I as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_I4 as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_SZARRAY as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_U1 as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_SZARRAY as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_U1 as COR_SIGNATURE,
        CorElementType::ELEMENT_TYPE_CLASS as COR_SIGNATURE,
    ];
    locals_signature.append(&mut compress_token(system_reflection_assembly_type_ref).unwrap());

    let locals_signature_token = module_metadata.emit.get_token_from_sig(&locals_signature)?;

    let instructions = vec![
        // Step 0) Check if the assembly was already loaded
        Instruction::call(already_loaded_method_token),
        // val is the offset of the instruction to go to when false
        Instruction::brfalse_s(Instruction::ret().len() as i8),
        Instruction::ret(),
        // Step 1) Call void GetAssemblyAndSymbolsBytes(out IntPtr assemblyPtr,
        // out int assemblySize, out IntPtr symbolsPtr, out int symbolsSize)
        Instruction::ldloca_s(0),
        Instruction::ldloca_s(1),
        Instruction::ldloca_s(2),
        Instruction::ldloca_s(3),
        Instruction::call(pinvoke_method_def),
        // Step 2) Call void Marshal.Copy(IntPtr source, byte[] destination,
        // int startIndex, int length) to populate the managed assembly bytes
        Instruction::ldloc_1(),
        Instruction::newarr(byte_type_ref),
        Instruction::stloc_s(4),
        Instruction::ldloc_0(),
        Instruction::ldloc_s(4),
        Instruction::ldc_i4_0(),
        Instruction::ldloc_1(),
        Instruction::call(marshal_copy_member_ref),
        // Step 3) Call void Marshal.Copy(IntPtr source, byte[] destination,
        // int startIndex, int length) to populate the symbols bytes
        Instruction::ldloc_3(),
        Instruction::newarr(byte_type_ref),
        Instruction::stloc_s(5),
        Instruction::ldloc_2(),
        Instruction::ldloc_s(5),
        Instruction::ldc_i4_0(),
        Instruction::ldloc_3(),
        Instruction::call(marshal_copy_member_ref),
        // Step 4) Call System.Reflection.Assembly System.AppDomain.CurrentDomain.Load(byte[], byte[]))
        Instruction::call(appdomain_get_current_domain_member_ref),
        Instruction::ldloc_s(4),
        Instruction::ldloc_s(5),
        Instruction::callvirt(appdomain_load_member_ref),
        Instruction::stloc_s(6),
        // Step 5) Call instance method Assembly.CreateInstance("Elastic.Apm.Profiler.Managed.Loader.Startup")
        Instruction::ldloc_s(6),
        Instruction::ldstr(load_helper_token),
        Instruction::callvirt(assembly_create_instance_member_ref),
        Instruction::pop(),
        Instruction::ret(),
    ];

    let method = Method {
        address: 0,
        header: MethodHeader::fat(
            false,
            false,
            instructions.iter().map(|i| i.opcode.len as u16).sum(),
            instructions.iter().map(|i| i.len() as u32).sum(),
            locals_signature_token
        ),
        instructions,
        sections: vec![],
    };

    let method_bytes = method.into_bytes();
    let allocated_bytes = allocator.alloc(method_bytes.len() as ULONG).map_err(|e| {
        log::warn!("generate_void_il_startup_method: failed to allocate memory for __ElasticVoidMethodCall__");
        e
    })?;

    let address = unsafe { allocated_bytes.into_inner() };
    unsafe {
        std::ptr::copy(method_bytes.as_ptr(), address, method_bytes.len());
    }
    log::trace!("generate_void_il_startup_method: write __ElasticVoidMethodCall__ body");
    profiler_info
        .set_il_function_body(module_id, new_method, address as *const _)
        .map_err(|e| {
            log::warn!(
                "generate_void_il_startup_method: failed to set il for __ElasticVoidMethodCall__"
            );
            e
        })?;

    Ok(new_method)
}
