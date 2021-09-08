// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

use crate::{
    cli::{uncompress_data, uncompress_token},
    ffi::{CorCallingConvention, CorElementType, E_FAIL},
    interfaces::IMetaDataImport2,
    profiler::types::ModuleMetadata,
    types::{MyFunctionInfo, MyTypeInfo},
};
use com::sys::HRESULT;
use num_traits::FromPrimitive;
use std::process::id;

fn parse_type_def_or_ref_encoded(signature: &[u8]) -> Option<usize> {
    let (token, len) = uncompress_token(signature);
    match len {
        0 => None,
        _ => Some(len),
    }
}

fn parse_custom_mod(signature: &[u8]) -> Option<usize> {
    if let Some(cor_element_type) = CorElementType::from_u8(signature[0]) {
        match cor_element_type {
            CorElementType::ELEMENT_TYPE_CMOD_OPT | CorElementType::ELEMENT_TYPE_CMOD_REQD => {
                let idx = 1_usize;
                if let Some(token_idx) = parse_type_def_or_ref_encoded(&signature[idx..]) {
                    Some(idx + token_idx)
                } else {
                    Some(idx)
                }
            }
            _ => None,
        }
    } else {
        None
    }
}

fn parse_optional_custom_mods(signature: &[u8]) -> Option<usize> {
    let mut idx = 0;

    loop {
        if let Some(cor_element_type) = CorElementType::from_u8(signature[idx]) {
            match cor_element_type {
                CorElementType::ELEMENT_TYPE_CMOD_OPT | CorElementType::ELEMENT_TYPE_CMOD_REQD => {
                    if let Some(mod_idx) = parse_custom_mod(&signature) {
                        idx += mod_idx;
                    } else {
                        return None;
                    }
                }
                _ => return Some(idx),
            }
        } else {
            return Some(idx);
        }
    }
}

pub fn parse_number(signature: &[u8]) -> Option<usize> {
    uncompress_data(signature).map(|(l, i)| i)
}

fn parse_ret_type(signature: &[u8]) -> Option<usize> {
    if let Some(mut idx) = parse_optional_custom_mods(&signature) {
        if let Some(cor_element_type) = CorElementType::from_u8(signature[idx]) {
            match cor_element_type {
                CorElementType::ELEMENT_TYPE_TYPEDBYREF | CorElementType::ELEMENT_TYPE_VOID => {
                    idx += 1;
                    return Some(idx);
                }
                CorElementType::ELEMENT_TYPE_BYREF => {
                    idx += 1;
                }
                _ => {}
            }
        }

        parse_type(&signature[idx..]).map(|i| idx + i)
    } else {
        None
    }
}

fn parse_method(signature: &[u8]) -> Option<usize> {
    let mut idx = 0;
    if let Some(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_GENERIC) =
        CorCallingConvention::from_bits(signature[idx])
    {
        idx += 1;
        if let Some(gen_idx) = parse_number(&signature[idx..]) {
            idx += gen_idx;
        } else {
            return None;
        }
    }

    let params;
    if let Some(param_idx) = parse_number(&signature[idx..]) {
        idx += param_idx;
        params = param_idx;
    } else {
        return None;
    }

    if let Some(ret_idx) = parse_ret_type(&signature[idx..]) {
        idx += ret_idx;
    } else {
        return None;
    }

    let mut sentinel_found = false;
    for _ in 0..params {
        if let Some(CorElementType::ELEMENT_TYPE_SENTINEL) = CorElementType::from_u8(signature[idx])
        {
            if sentinel_found {
                return None;
            }

            sentinel_found = true;
            idx += 1;
        }

        if let Some(param_idx) = parse_param(&signature[idx..]) {
            idx += param_idx;
        } else {
            return None;
        }
    }

    Some(idx)
}

fn parse_param(signature: &[u8]) -> Option<usize> {
    if let Some(mut idx) = parse_optional_custom_mods(&signature) {
        if let Some(cor_element_type) = CorElementType::from_u8(signature[idx]) {
            match cor_element_type {
                CorElementType::ELEMENT_TYPE_TYPEDBYREF => {
                    idx += 1;
                    return Some(idx);
                }
                CorElementType::ELEMENT_TYPE_BYREF => {
                    idx += 1;
                }
                _ => {}
            }
        }

        parse_type(&signature[idx..]).map(|i| idx + i)
    } else {
        None
    }
}

fn parse_array_shape(signature: &[u8]) -> Option<usize> {
    let mut idx = 0;

    if let Some(rank_idx) = parse_number(&signature[idx..]) {
        idx += rank_idx;
    } else {
        return None;
    }

    let num_sizes;
    if let Some(numsize_idx) = parse_number(&signature[idx..]) {
        idx += numsize_idx;
        num_sizes = numsize_idx;
    } else {
        return None;
    }

    for _ in 0..num_sizes {
        if let Some(size_idx) = parse_number(&signature[idx..]) {
            idx += size_idx;
        } else {
            return None;
        }
    }

    if let Some(size_idx) = parse_number(&signature[idx..]) {
        idx += size_idx;
    } else {
        return None;
    }

    for _ in 0..num_sizes {
        if let Some(size_idx) = parse_number(&signature[idx..]) {
            idx += size_idx;
        } else {
            return None;
        }
    }

    Some(idx)
}

pub fn parse_type(signature: &[u8]) -> Option<usize> {
    let mut idx = 0;
    if let Some(cor_element_type) = CorElementType::from_u8(signature[idx]) {
        idx += 1;

        match cor_element_type {
            CorElementType::ELEMENT_TYPE_VOID
            | CorElementType::ELEMENT_TYPE_BOOLEAN
            | CorElementType::ELEMENT_TYPE_CHAR
            | CorElementType::ELEMENT_TYPE_I1
            | CorElementType::ELEMENT_TYPE_U1
            | CorElementType::ELEMENT_TYPE_I2
            | CorElementType::ELEMENT_TYPE_U2
            | CorElementType::ELEMENT_TYPE_I4
            | CorElementType::ELEMENT_TYPE_U4
            | CorElementType::ELEMENT_TYPE_I8
            | CorElementType::ELEMENT_TYPE_U8
            | CorElementType::ELEMENT_TYPE_R4
            | CorElementType::ELEMENT_TYPE_R8
            | CorElementType::ELEMENT_TYPE_STRING
            | CorElementType::ELEMENT_TYPE_OBJECT => Some(idx),
            CorElementType::ELEMENT_TYPE_PTR => {
                if let Some(mods_idx) = parse_optional_custom_mods(&signature[idx..]) {
                    idx += mods_idx;
                    if let Some(CorElementType::ELEMENT_TYPE_VOID) =
                        CorElementType::from_u8(signature[idx])
                    {
                        idx += 1;
                        Some(idx)
                    } else {
                        parse_type(&signature[idx..]).map(|i| idx + i)
                    }
                } else {
                    None
                }
            }
            CorElementType::ELEMENT_TYPE_VALUETYPE | CorElementType::ELEMENT_TYPE_CLASS => {
                parse_type_def_or_ref_encoded(&signature[idx..]).map(|i| idx + i)
            }
            CorElementType::ELEMENT_TYPE_FNPTR => parse_method(&signature[idx..]).map(|i| idx + i),
            CorElementType::ELEMENT_TYPE_ARRAY => {
                if let Some(type_idx) = parse_type(&signature[idx..]) {
                    idx += type_idx;
                } else {
                    return None;
                }

                parse_array_shape(&signature[idx..]).map(|i| idx + i)
            }
            CorElementType::ELEMENT_TYPE_SZARRAY => {
                if let Some(mods_idx) = parse_optional_custom_mods(&signature[idx..]) {
                    idx += mods_idx;
                    parse_type(&signature[idx..]).map(|i| idx + i)
                } else {
                    None
                }
            }
            CorElementType::ELEMENT_TYPE_GENERICINST => {
                if let Some(elem_type) = CorElementType::from_u8(signature[idx]) {
                    match elem_type {
                        CorElementType::ELEMENT_TYPE_VALUETYPE
                        | CorElementType::ELEMENT_TYPE_CLASS => {
                            idx += 1;

                            if let Some(type_idx) = parse_type_def_or_ref_encoded(&signature[idx..])
                            {
                                idx += type_idx;
                            } else {
                                return None;
                            }

                            let num;
                            if let Some(num_idx) = parse_number(&signature[idx..]) {
                                idx += num_idx;
                                num = num_idx;
                            } else {
                                return None;
                            }

                            for _ in 0..num {
                                if let Some(type_idx) = parse_type(&signature[idx..]) {
                                    idx += type_idx;
                                } else {
                                    return None;
                                }
                            }

                            Some(idx)
                        }
                        _ => None,
                    }
                } else {
                    None
                }
            }
            CorElementType::ELEMENT_TYPE_VAR | CorElementType::ELEMENT_TYPE_MVAR => {
                parse_number(&signature[idx..]).map(|i| idx + i)
            }
            _ => None,
        }
    } else {
        None
    }
}

fn retrieve_type_for_signature(
    metadata_import: &IMetaDataImport2,
    signature: &[u8],
) -> Result<(MyTypeInfo, usize), HRESULT> {
    let (token, len) = uncompress_token(signature);
    match metadata_import.get_type_info(token) {
        Ok(Some(type_info)) => Ok((type_info, len)),
        Ok(None) => {
            log::warn!(
                "None type info from token={} in signature={:?}",
                token,
                signature
            );
            Err(E_FAIL)
        }
        Err(e) => {
            log::warn!(
                "Could not get type info from token={} in signature={:?}, {}",
                token,
                signature,
                e
            );
            Err(e)
        }
    }
}

pub fn parse_signature_types(
    module_metadata: &ModuleMetadata,
    function_info: &MyFunctionInfo,
) -> Option<Vec<String>> {
    let signature = &function_info.signature;
    let signature_size = signature.len();
    let generic_count = signature.type_arguments_len();
    let param_count = signature.arguments_len();
    let start_index = if generic_count > 0 { 3 } else { 2 };

    let expected_number_of_types = param_count + 1;
    let mut current_type_index = 0;
    let mut type_names = Vec::with_capacity(expected_number_of_types as usize);

    let mut generic_arg_stack = Vec::new();
    let mut append_to_type = String::new();
    let mut current_type_name = String::new();

    let signature = signature.bytes();
    let params = &signature[start_index..];
    let mut enumerator = params.iter().enumerate().peekable();

    while let Some((i, param_piece)) = enumerator.next() {
        let cor_element_type: CorElementType = CorElementType::from_u8(*param_piece)?;
        match cor_element_type {
            CorElementType::ELEMENT_TYPE_END => {
                continue;
            }
            CorElementType::ELEMENT_TYPE_VOID => current_type_name.push_str("System.Void"),
            CorElementType::ELEMENT_TYPE_BOOLEAN => current_type_name.push_str("System.Boolean"),
            CorElementType::ELEMENT_TYPE_CHAR => current_type_name.push_str("System.Char"),
            CorElementType::ELEMENT_TYPE_I1 => current_type_name.push_str("System.SByte"),
            CorElementType::ELEMENT_TYPE_U1 => current_type_name.push_str("System.Byte"),
            CorElementType::ELEMENT_TYPE_I2 => current_type_name.push_str("System.Int16"),
            CorElementType::ELEMENT_TYPE_U2 => current_type_name.push_str("System.UInt16"),
            CorElementType::ELEMENT_TYPE_I4 => current_type_name.push_str("System.Int32"),
            CorElementType::ELEMENT_TYPE_U4 => current_type_name.push_str("System.UInt32"),
            CorElementType::ELEMENT_TYPE_I8 => current_type_name.push_str("System.Int64"),
            CorElementType::ELEMENT_TYPE_U8 => current_type_name.push_str("System.UInt64"),
            CorElementType::ELEMENT_TYPE_R4 => current_type_name.push_str("System.Single"),
            CorElementType::ELEMENT_TYPE_R8 => current_type_name.push_str("System.Double"),
            CorElementType::ELEMENT_TYPE_STRING => current_type_name.push_str("System.String"),
            CorElementType::ELEMENT_TYPE_VALUETYPE | CorElementType::ELEMENT_TYPE_CLASS => {
                let (j, next) = enumerator.next().unwrap();
                let type_info = retrieve_type_for_signature(&module_metadata.import, &params[j..]);
                if type_info.is_err() {
                    return None;
                }
                let (type_data, len) = type_info.unwrap();
                let mut examined_type_token = type_data.id;
                let mut examined_type_name = type_data.name;
                let mut ongoing_type_name = examined_type_name.to_string();

                // check for nested class
                while examined_type_name.contains('.') {
                    let parent_token = module_metadata
                        .import
                        .get_nested_class_props(examined_type_token);
                    if parent_token.is_err() {
                        break;
                    }
                    let parent_token = parent_token.unwrap();
                    let nesting_type = module_metadata.import.get_type_info(parent_token);
                    if nesting_type.is_err() {
                        log::warn!(
                            "Could not retrieve type info for parent token {}",
                            parent_token
                        );
                        return None;
                    }
                    let nesting_type = match nesting_type.unwrap() {
                        Some(n) => n,
                        None => {
                            return None;
                        }
                    };

                    examined_type_token = nesting_type.id;
                    examined_type_name = nesting_type.name;
                    ongoing_type_name = format!("{}+{}", &examined_type_name, &ongoing_type_name);
                }

                // skip len number of items
                for _ in 0..(len - 1) {
                    enumerator.next();
                }
                current_type_name.push_str(&ongoing_type_name);
            }
            CorElementType::ELEMENT_TYPE_BYREF => current_type_name.push_str("ref"),
            CorElementType::ELEMENT_TYPE_GENERICINST => {
                // skip generic type indicator token
                let _ = enumerator.next();
                // skip generic type token
                let (j, next) = enumerator.next().unwrap();
                let generic_type_info =
                    retrieve_type_for_signature(&module_metadata.import, &params[j..]);
                if generic_type_info.is_err() {
                    return None;
                }
                let (generic_type_info, len) = generic_type_info.unwrap();
                let type_name = &generic_type_info.name;
                current_type_name.push_str(type_name);
                current_type_name.push('<');

                if !generic_arg_stack.is_empty() {
                    generic_arg_stack[0] -= 1;
                }

                let arity_index = type_name.rfind('`').unwrap();
                let actual_args = type_name[(arity_index + 1)..].parse::<i32>().unwrap();
                generic_arg_stack.insert(0, actual_args);

                for _ in 0..len {
                    enumerator.next();
                }
                continue;
            }
            CorElementType::ELEMENT_TYPE_OBJECT => current_type_name.push_str("System.Object"),
            CorElementType::ELEMENT_TYPE_SZARRAY => {
                append_to_type.push_str("[]");
                while let Some((j, next)) = enumerator.peek() {
                    // check it's an array element type
                    if let Some(CorElementType::ELEMENT_TYPE_SZARRAY) =
                        CorElementType::from_u8(**next)
                    {
                        append_to_type.push_str("[]");
                        enumerator.next();
                    } else {
                        break;
                    }
                }

                continue;
            }
            CorElementType::ELEMENT_TYPE_MVAR | CorElementType::ELEMENT_TYPE_VAR => {
                let (token, len) = uncompress_token(&params[i..]);
                current_type_name.push('T');
                // skip len number of items
                for _ in 0..len {
                    enumerator.next();
                }
                // TODO: implement conventions for generics (eg., TC1, TC2, TM1, TM2)
                // current_type_name.push(type_token.to_string());
            }
            _ => {
                log::warn!("Unexpected element type: {:?}", cor_element_type);
                current_type_name.push_str(&format!("{}", cor_element_type as u32));
            }
        }

        if !append_to_type.is_empty() {
            current_type_name.push_str(&append_to_type);
            append_to_type = String::new();
        }

        if !generic_arg_stack.is_empty() {
            generic_arg_stack[0] -= 1;

            if generic_arg_stack[0] > 0 {
                current_type_name.push_str(", ");
            }
        }

        while !generic_arg_stack.is_empty() && generic_arg_stack[0] == 0 {
            generic_arg_stack.remove(0);
            current_type_name.push('>');

            if !generic_arg_stack.is_empty() && generic_arg_stack[0] > 0 {
                current_type_name.push_str(", ");
            }
        }

        if !generic_arg_stack.is_empty() {
            continue;
        }

        if current_type_index >= expected_number_of_types {
            return None;
        }

        type_names.push(current_type_name);
        current_type_name = String::new();
        current_type_index += 1;
    }

    Some(type_names)
}

pub fn get_sig_type_token_name(
    signature: &[u8],
    metadata_import: &IMetaDataImport2,
) -> (String, usize) {
    let mut token_name = String::new();
    let mut ref_flag = false;
    let mut idx = 0;
    if signature[idx] == CorElementType::ELEMENT_TYPE_BYREF as u8 {
        idx += 1;
        ref_flag = true;
    }

    if let Some(elem_type) = CorElementType::from_u8(signature[idx]) {
        match elem_type {
            CorElementType::ELEMENT_TYPE_BOOLEAN => token_name.push_str("System.Boolean"),
            CorElementType::ELEMENT_TYPE_CHAR => token_name.push_str("System.Char"),
            CorElementType::ELEMENT_TYPE_I1 => token_name.push_str("System.SByte"),
            CorElementType::ELEMENT_TYPE_U1 => token_name.push_str("System.Byte"),
            CorElementType::ELEMENT_TYPE_I2 => token_name.push_str("System.Int16"),
            CorElementType::ELEMENT_TYPE_U2 => token_name.push_str("System.UInt16"),
            CorElementType::ELEMENT_TYPE_I4 => token_name.push_str("System.Int32"),
            CorElementType::ELEMENT_TYPE_U4 => token_name.push_str("System.UInt32"),
            CorElementType::ELEMENT_TYPE_I8 => token_name.push_str("System.Int64"),
            CorElementType::ELEMENT_TYPE_U8 => token_name.push_str("System.UInt64"),
            CorElementType::ELEMENT_TYPE_R4 => token_name.push_str("System.Single"),
            CorElementType::ELEMENT_TYPE_R8 => token_name.push_str("System.Double"),
            CorElementType::ELEMENT_TYPE_STRING => token_name.push_str("System.String"),
            CorElementType::ELEMENT_TYPE_OBJECT => token_name.push_str("System.Object"),
            CorElementType::ELEMENT_TYPE_CLASS | CorElementType::ELEMENT_TYPE_VALUETYPE => {
                idx += 1;
                let (token, len) = uncompress_token(&signature[idx..]);
                idx += len;
                if let Ok(Some(type_info)) = metadata_import.get_type_info(token) {
                    token_name.push_str(&type_info.name);
                }
            }
            CorElementType::ELEMENT_TYPE_SZARRAY => {
                idx += 1;
                let elem = get_sig_type_token_name(&signature[idx..], metadata_import);
                token_name.push_str(&elem.0);
                token_name.push_str("[]");
            }
            CorElementType::ELEMENT_TYPE_GENERICINST => {
                idx += 1;
                let (elem, end_idx) = get_sig_type_token_name(&signature[idx..], metadata_import);
                idx += end_idx;
                token_name.push_str(&elem);
                token_name.push('[');

                if let Some((data, len)) = uncompress_data(&signature[idx..]) {
                    idx += data as usize;
                    for i in 0..len {
                        let (elem, end_idx) =
                            get_sig_type_token_name(&signature[idx..], metadata_import);
                        idx += end_idx;
                        token_name.push_str(&elem);
                        if i != (len - 1) {
                            token_name.push(',');
                        }
                    }
                }
                token_name.push(']');
            }
            CorElementType::ELEMENT_TYPE_MVAR => {
                idx += 1;
                if let Some((data, len)) = uncompress_data(&signature[idx..]) {
                    idx += data as usize;
                    token_name.push_str("!!");
                    token_name.push_str(&format!("{}", len));
                }
            }
            CorElementType::ELEMENT_TYPE_VAR => {
                idx += 1;
                if let Some((data, len)) = uncompress_data(&signature[idx..]) {
                    idx += data as usize;
                    token_name.push('!');
                    token_name.push_str(&format!("{}", len));
                }
            }
            _ => {}
        }
    } else {
        return (token_name, idx);
    }

    if ref_flag {
        token_name.push('&');
    }

    (token_name, idx)
}
