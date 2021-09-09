// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use com::sys::HRESULT;
use num_traits::FromPrimitive;

use crate::{
    cil::{
        uncompress_data, uncompress_token, CorExceptionFlag, Method, Operand,
        Operand::{
            InlineBrTarget, InlineField, InlineI, InlineI8, InlineMethod, InlineString, InlineType,
            ShortInlineBrTarget, ShortInlineI, ShortInlineVar,
        },
        Section, BOX, CALL, CALLVIRT, CASTCLASS, INITOBJ, LDSTR, NEWARR, NEWOBJ, UNBOX_ANY,
    },
    ffi::{
        mdAssemblyRef, mdToken, mdTokenNil, mdTypeDef, mdTypeDefNil, type_from_token,
        CorAssemblyFlags, CorElementType, CorTokenType, ModuleID, ASSEMBLYMETADATA,
    },
    interfaces::{IMetaDataAssemblyEmit, IMetaDataEmit2, IMetaDataImport2},
    profiler::{
        sig::parse_type,
        types::{
            MethodSignature, ModuleMetadata, FunctionInfo, WrapperMethodRef,
            WrapperMethodReference,
        },
    },
};

pub(crate) fn return_type_is_value_type_or_generic(
    metadata_import: &IMetaDataImport2,
    metadata_emit: &IMetaDataEmit2,
    assembly_emit: &IMetaDataAssemblyEmit,
    target_function_token: mdToken,
    target_function_signature: &MethodSignature,
) -> Option<mdToken> {
    let generic_count = target_function_signature.type_arguments_len();
    let mut method_def_sig_index = target_function_signature.index_of_return_type();
    let signature = target_function_signature.bytes();
    let ret_type_byte = signature[method_def_sig_index];
    let spec_signature;

    if let Some(ret_type) = CorElementType::from_u8(ret_type_byte) {
        match ret_type {
            CorElementType::ELEMENT_TYPE_VOID => {
                return None;
            }
            CorElementType::ELEMENT_TYPE_GENERICINST => {
                return if let Some(CorElementType::ELEMENT_TYPE_VALUETYPE) =
                    CorElementType::from_u8(signature[method_def_sig_index + 1])
                {
                    let start_idx = method_def_sig_index;
                    let mut end_idx = start_idx;
                    if let Some(type_idx) = parse_type(&signature[method_def_sig_index..]) {
                        end_idx += type_idx;
                        metadata_emit
                            .get_token_from_type_spec(&signature[start_idx..end_idx])
                            .ok()
                    } else {
                        None
                    }
                } else {
                    None
                }
            }
            CorElementType::ELEMENT_TYPE_VAR | CorElementType::ELEMENT_TYPE_MVAR => {
                method_def_sig_index += 1;
                return if let Some((generic_type_index, generic_type_len)) =
                    uncompress_data(&signature[method_def_sig_index..])
                {
                    let generic_type_index = generic_type_index as usize;
                    let token_type = {
                        let t = type_from_token(target_function_token);
                        CorTokenType::from_bits(t).unwrap()
                    };
                    let parent_token;
                    match token_type {
                        CorTokenType::mdtMemberRef => {
                            if generic_count > 0 {
                                return None;
                            }

                            if let Ok(member_ref_props) =
                                metadata_import.get_member_ref_props(target_function_token)
                            {
                                parent_token = member_ref_props.class_token;
                                if let Ok(type_spec) =
                                    metadata_import.get_type_spec_from_token(parent_token)
                                {
                                    spec_signature = type_spec.signature;
                                } else {
                                    log::warn!("element_type={:?}: failed to get parent token or signature", ret_type);
                                    return None;
                                }
                            } else {
                                log::warn!(
                                    "element_type={:?}: failed to get parent token or signature",
                                    ret_type
                                );
                                return None;
                            }
                        }
                        CorTokenType::mdtMethodDef => {
                            if generic_count > 0 {
                                return None;
                            }

                            if let Ok(member_props) =
                                metadata_import.get_member_props(target_function_token)
                            {
                                parent_token = member_props.class_token;
                                if let Ok(type_spec) =
                                    metadata_import.get_type_spec_from_token(parent_token)
                                {
                                    spec_signature = type_spec.signature;
                                } else {
                                    log::warn!("element_type={:?}: failed to get parent token or signature", ret_type);
                                    return None;
                                }
                            } else {
                                log::warn!(
                                    "element_type={:?}: failed to get parent token or signature",
                                    ret_type
                                );
                                return None;
                            }
                        }
                        CorTokenType::mdtMethodSpec => {
                            if let Ok(method_spec_props) =
                                metadata_import.get_method_spec_props(target_function_token)
                            {
                                parent_token = method_spec_props.parent;
                                spec_signature = method_spec_props.signature;
                            } else {
                                log::warn!(
                                    "element_type={:?}: failed to get parent token or signature",
                                    ret_type
                                );
                                return None;
                            }
                        }
                        _ => {
                            log::trace!("hit NONE");
                            // TODO: logging
                            return None;
                        }
                    }

                    let mut parent_token_index = 0;
                    if token_type == CorTokenType::mdtMemberRef
                        || token_type == CorTokenType::mdtMethodDef
                    {
                        parent_token_index = 2;
                        let (token, len) = uncompress_token(&spec_signature[parent_token_index..]);
                        parent_token_index += len;
                    } else if token_type == CorTokenType::mdtMethodSpec {
                        parent_token_index = 1;
                    }

                    let mut num_generic_arguments = 0;
                    if let Some((token, len)) =
                        uncompress_data(&spec_signature[parent_token_index..])
                    {
                        parent_token_index += len;
                        num_generic_arguments = token as usize;
                    }

                    let mut current_idx = parent_token_index;

                    for i in 0..num_generic_arguments {
                        if i != generic_type_index {
                            if let Some(type_idx) = parse_type(&spec_signature[current_idx..]) {
                                current_idx += type_idx;
                            } else {
                                log::warn!("element_type={:?}: unable to parse generic type argument {} from parent token signature {}", ret_type, i, parent_token);
                                return None;
                            }
                        } else if let Some(element_type) =
                            CorElementType::from_u8(spec_signature[current_idx])
                        {
                            return match element_type {
                                CorElementType::ELEMENT_TYPE_MVAR
                                | CorElementType::ELEMENT_TYPE_VAR => metadata_emit
                                    .get_token_from_type_spec(
                                        &spec_signature[current_idx..(current_idx + 2)],
                                    )
                                    .ok(),
                                CorElementType::ELEMENT_TYPE_GENERICINST => {
                                    if let Some(CorElementType::ELEMENT_TYPE_VALUETYPE) =
                                        CorElementType::from_u8(spec_signature[current_idx + 1])
                                    {
                                        let start_idx = current_idx;
                                        let mut end_idx = start_idx;
                                        if let Some(type_idx) =
                                            parse_type(&spec_signature[end_idx..])
                                        {
                                            end_idx += type_idx;
                                        } else {
                                            return None;
                                        }
                                        metadata_emit
                                            .get_token_from_type_spec(
                                                &spec_signature[start_idx..end_idx],
                                            )
                                            .ok()
                                    } else {
                                        None
                                    }
                                }
                                _ => return_type_token_for_value_type_element_type(
                                    &spec_signature[current_idx..],
                                    metadata_emit,
                                    assembly_emit,
                                ),
                            };
                        } else {
                            return return_type_token_for_value_type_element_type(
                                &spec_signature[current_idx..],
                                metadata_emit,
                                assembly_emit,
                            );
                        }
                    }

                    None
                } else {
                    log::warn!(
                        "element_type={:?}: unable to retrieve VAR|MVAR index",
                        ret_type
                    );
                    None
                };
            }
            _ => {}
        }
    }

    return_type_token_for_value_type_element_type(
        &signature[method_def_sig_index..],
        metadata_emit,
        assembly_emit,
    )
}

fn return_type_token_for_value_type_element_type(
    signature: &[u8],
    metadata_emit: &IMetaDataEmit2,
    assembly_emit: &IMetaDataAssemblyEmit,
) -> Option<mdToken> {
    log::trace!(
        "return_type_token_for_value_type_element_type, signature: {:?}",
        signature
    );

    if let Some(cor_element_type) = CorElementType::from_u8(signature[0]) {
        let mut managed_type_name = String::new();
        match cor_element_type {
            CorElementType::ELEMENT_TYPE_VALUETYPE => {
                let (token, len) = uncompress_token(&signature[1..]);
                return if len > 0 {
                    Some(token)
                } else {
                    log::warn!(
                        "ELEMENT_TYPE_VALUETYPE failed to find uncompress TypeRef or TypeDef"
                    );
                    None
                };
            }
            CorElementType::ELEMENT_TYPE_VOID => managed_type_name.push_str("System.Void"),
            CorElementType::ELEMENT_TYPE_BOOLEAN => managed_type_name.push_str("System.Boolean"),
            CorElementType::ELEMENT_TYPE_CHAR => managed_type_name.push_str("System.Char"),
            CorElementType::ELEMENT_TYPE_I1 => managed_type_name.push_str("System.SByte"),
            CorElementType::ELEMENT_TYPE_U1 => managed_type_name.push_str("System.Byte"),
            CorElementType::ELEMENT_TYPE_I2 => managed_type_name.push_str("System.Int16"),
            CorElementType::ELEMENT_TYPE_U2 => managed_type_name.push_str("System.UInt16"),
            CorElementType::ELEMENT_TYPE_I4 => managed_type_name.push_str("System.Int32"),
            CorElementType::ELEMENT_TYPE_U4 => managed_type_name.push_str("System.UInt32"),
            CorElementType::ELEMENT_TYPE_I8 => managed_type_name.push_str("System.Int64"),
            CorElementType::ELEMENT_TYPE_U8 => managed_type_name.push_str("System.UInt64"),
            CorElementType::ELEMENT_TYPE_R4 => managed_type_name.push_str("System.Single"),
            CorElementType::ELEMENT_TYPE_R8 => managed_type_name.push_str("System.Double"),
            CorElementType::ELEMENT_TYPE_TYPEDBYREF => {
                managed_type_name.push_str("System.TypedReference")
            }
            CorElementType::ELEMENT_TYPE_I => managed_type_name.push_str("System.IntPtr"),
            CorElementType::ELEMENT_TYPE_U => managed_type_name.push_str("System.UIntPtr"),
            _ => {
                return None;
            }
        }

        if managed_type_name.is_empty() {
            log::warn!("no managed type name given");
            return None;
        }

        let mscorlib_ref = create_assembly_ref_to_mscorlib(assembly_emit);
        if mscorlib_ref.is_err() {
            log::warn!("failed to define AssemblyRef to mscorlib");
            return None;
        }

        let mscorlib_ref = mscorlib_ref.unwrap();
        let type_ref = metadata_emit.define_type_ref_by_name(mscorlib_ref, &managed_type_name);
        if type_ref.is_err() {
            log::warn!(
                "unable to create type ref for managed_type_name={}",
                &managed_type_name
            );
            return None;
        }
        let type_ref = type_ref.unwrap();
        Some(type_ref)
    } else {
        None
    }
}

pub fn create_assembly_ref_to_mscorlib(
    assembly_emit: &IMetaDataAssemblyEmit,
) -> Result<mdAssemblyRef, HRESULT> {
    let assembly_metadata = ASSEMBLYMETADATA {
        usMajorVersion: 4,
        usMinorVersion: 0,
        usBuildNumber: 0,
        usRevisionNumber: 0,
        szLocale: std::ptr::null_mut(),
        cbLocale: 0,
        rProcessor: std::ptr::null_mut(),
        ulProcessor: 0,
        rOS: std::ptr::null_mut(),
        ulOS: 0,
    };

    let public_key: &[u8; 8] = &[0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89];
    assembly_emit.define_assembly_ref(
        public_key,
        "mscorlib",
        &assembly_metadata,
        &[],
        CorAssemblyFlags::empty(),
    )
}

pub fn get_il_codes(
    title: &str,
    method: &Method,
    caller: &FunctionInfo,
    module_metadata: &ModuleMetadata,
) -> String {
    let mut buf = String::new();
    buf.push_str(title);
    buf.push_str(caller.type_info.as_ref().map_or("", |t| t.name.as_str()));
    buf.push('.');
    buf.push_str(&caller.name);
    buf.push_str(&format!(" => (max_stack: {})", method.header.max_stack()));

    let local_sig = method.header.local_var_sig_tok();
    if local_sig != mdTokenNil {
        if let Ok(signature) = module_metadata.import.get_sig_from_token(local_sig) {
            buf.push('\n');
            buf.push_str(". Local Var Signature ");
            buf.push_str(hex::encode(signature).as_str());
            buf.push('\n');
        }
    }

    buf.push('\n');

    let mut address = method.address;
    let mut sum_len = 0;
    let mut indent = 1;

    for (idx, instruction) in method.instructions.iter().enumerate() {
        for section in &method.sections {
            match section {
                Section::FatSection(h, s) => {
                    for ss in s {
                        if ss.flag == CorExceptionFlag::COR_ILEXCEPTION_CLAUSE_FINALLY {
                            if ss.try_offset as usize == sum_len {
                                if indent > 0 {
                                    buf.push_str(&"  ".repeat(indent));
                                }
                                buf.push_str(".try {\n");
                                indent += 1;
                            }
                            if (ss.try_offset + ss.try_length) as usize == sum_len {
                                indent -= 1;
                                if indent > 0 {
                                    buf.push_str(&"  ".repeat(indent));
                                }
                                buf.push_str("}\n");
                            }
                            if ss.handler_offset as usize == sum_len {
                                if indent > 0 {
                                    buf.push_str(&"  ".repeat(indent));
                                }
                                buf.push_str(".finally {\n");
                                indent += 1;
                            }
                        }
                    }
                }
                Section::SmallSection(h, s) => {
                    for ss in s {
                        if ss.flag == CorExceptionFlag::COR_ILEXCEPTION_CLAUSE_FINALLY {
                            if ss.try_offset as usize == sum_len {
                                if indent > 0 {
                                    buf.push_str(&"  ".repeat(indent));
                                }
                                buf.push_str(".try {\n");
                                indent += 1;
                            }
                            if (ss.try_offset + ss.try_length as u16) as usize == sum_len {
                                indent -= 1;
                                if indent > 0 {
                                    buf.push_str(&"  ".repeat(indent));
                                }
                                buf.push_str("}\n");
                            }
                            if ss.handler_offset as usize == sum_len {
                                if indent > 0 {
                                    buf.push_str(&"  ".repeat(indent));
                                }
                                buf.push_str(".finally {\n");
                                indent += 1;
                            }
                        }
                    }
                }
            }
        }

        for section in &method.sections {
            match section {
                Section::FatSection(h, s) => {
                    for ss in s {
                        if ss.flag == CorExceptionFlag::COR_ILEXCEPTION_CLAUSE_NONE {
                            if ss.try_offset as usize == sum_len {
                                if indent > 0 {
                                    buf.push_str(&"  ".repeat(indent));
                                }
                                buf.push_str(".try {\n");
                                indent += 1;
                            }
                            if (ss.try_offset + ss.try_length) as usize == sum_len {
                                indent -= 1;
                                if indent > 0 {
                                    buf.push_str(&"  ".repeat(indent));
                                }
                                buf.push_str("}\n");
                            }
                            if ss.handler_offset as usize == sum_len {
                                if indent > 0 {
                                    buf.push_str(&"  ".repeat(indent));
                                }
                                buf.push_str(".catch {\n");
                                indent += 1;
                            }
                        }
                    }
                }
                Section::SmallSection(h, s) => {
                    for ss in s {
                        if ss.flag == CorExceptionFlag::COR_ILEXCEPTION_CLAUSE_NONE {
                            if ss.try_offset as usize == sum_len {
                                if indent > 0 {
                                    buf.push_str(&"  ".repeat(indent));
                                }
                                buf.push_str(".try {\n");
                                indent += 1;
                            }
                            if (ss.try_offset + ss.try_length as u16) as usize == sum_len {
                                indent -= 1;
                                if indent > 0 {
                                    buf.push_str(&"  ".repeat(indent));
                                }
                                buf.push_str("}\n");
                            }
                            if ss.handler_offset as usize == sum_len {
                                if indent > 0 {
                                    buf.push_str(&"  ".repeat(indent));
                                }
                                buf.push_str(".catch {\n");
                                indent += 1;
                            }
                        }
                    }
                }
            }
        }

        if indent > 0 {
            buf.push_str(&"  ".repeat(indent));
        }

        buf.push_str(&format!("{} {:>10}", address, instruction.opcode.name));

        if instruction.opcode == CALL
            || instruction.opcode == CALLVIRT
            || instruction.opcode == NEWOBJ
        {
            if let InlineMethod(token) = instruction.operand {
                buf.push_str(&format!(" {}", token));
                if let Ok(member_info) = module_metadata.import.get_function_info(token) {
                    buf.push_str("  | ");
                    buf.push_str(member_info.full_name().as_str());
                    if member_info.signature.arguments_len() > 0 {
                        buf.push_str(&format!(
                            "({} argument{{s}})",
                            member_info.signature.arguments_len()
                        ));
                    } else {
                        buf.push_str("()");
                    }
                }
            }
        } else if instruction.opcode == CASTCLASS
            || instruction.opcode == BOX
            || instruction.opcode == UNBOX_ANY
            || instruction.opcode == NEWARR
            || instruction.opcode == INITOBJ
        {
            if let InlineType(token) = instruction.operand {
                buf.push_str(&format!(" {}", token));
                if let Ok(type_info) = module_metadata.import.get_type_info(token) {
                    if let Some(t) = type_info {
                        buf.push_str("  | ");
                        buf.push_str(&t.name);
                    } else {
                        buf.push_str(&format!(" {}", token));
                    }
                }
            }
        } else if instruction.opcode == LDSTR {
            if let InlineString(token) = instruction.operand {
                buf.push_str(&format!(" {}", token));
                if let Ok(str) = module_metadata.import.get_user_string(token) {
                    buf.push_str("  | \"");
                    buf.push_str(&str);
                    buf.push('"');
                }
            }
        } else if let InlineI8(arg) = instruction.operand {
            buf.push_str(&format!(" {}", arg));
        } else if let InlineBrTarget(t) = instruction.operand {
            buf.push_str(&format!(
                " {}",
                address as i64 + (t as i64) + instruction.len() as i64
            ));
        } else if let ShortInlineBrTarget(t) = instruction.operand {
            buf.push_str(&format!(
                " {}",
                address as i64 + (t as i64) + instruction.len() as i64
            ));
        } else if let ShortInlineVar(arg) = instruction.operand {
            buf.push_str(&format!(" {}", arg));
        } else if let ShortInlineI(arg) = instruction.operand {
            buf.push_str(&format!(" {}", arg));
        } else if let InlineI(arg) = instruction.operand {
            buf.push_str(&format!(" {}", arg));
        } else if let InlineField(arg) = instruction.operand {
            buf.push_str(&format!(" {}", arg));
        }

        buf.push('\n');
        sum_len += instruction.len();

        for section in &method.sections {
            match section {
                Section::FatSection(h, s) => {
                    for ss in s {
                        if (ss.handler_offset + ss.handler_length) as usize == sum_len {
                            indent -= 1;
                            if indent > 0 {
                                buf.push_str(&"  ".repeat(indent));
                            }
                            buf.push_str("}\n");
                        }
                    }
                }
                Section::SmallSection(h, s) => {
                    for ss in s {
                        if (ss.handler_offset + ss.handler_length as u16) as usize == sum_len {
                            indent -= 1;
                            if indent > 0 {
                                buf.push_str(&"  ".repeat(indent));
                            }
                            buf.push_str("}\n");
                        }
                    }
                }
            }
        }

        address += instruction.len();
    }

    buf
}

pub fn find_type_def_by_name(
    target_method_type_name: &str,
    assembly_name: &str,
    metadata_import: &IMetaDataImport2,
) -> Option<mdTypeDef> {
    let parts: Vec<&str> = target_method_type_name.split('+').collect();
    let method_type_name;
    let mut parent = mdTypeDefNil;
    match parts.len() {
        1 => method_type_name = target_method_type_name,
        2 => {
            method_type_name = parts[1];
            if let Ok(parent_type_def) = metadata_import.find_type_def_by_name(parts[0], None) {
                parent = parent_type_def;
            } else {
                return None;
            }
        }
        _ => {
            log::warn!(
                "Invalid type def- only one layer of nested classes are supported: {}, module={}",
                target_method_type_name,
                assembly_name
            );
            return None;
        }
    }

    if let Ok(type_def) = metadata_import.find_type_def_by_name(method_type_name, Some(parent)) {
        Some(type_def)
    } else {
        log::debug!(
            "Cannot find type_def={}, module={}",
            method_type_name,
            assembly_name
        );
        None
    }
}
