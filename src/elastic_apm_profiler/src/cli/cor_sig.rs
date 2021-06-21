use crate::ffi::{
    mdToken, mdTokenNil, rid_from_token, token_from_rid, type_from_token, CorTokenType, BYTE,
    DWORD, ULONG,
};

const ENCODE_TOKEN: [mdToken; 4] = [
    CorTokenType::mdtTypeDef.bits(),
    CorTokenType::mdtTypeRef.bits(),
    CorTokenType::mdtTypeSpec.bits(),
    CorTokenType::mdtBaseType.bits(),
];

/// Compress a token.
/// The least significant bit of the first compress byte will indicate the token type.
pub fn compress_token(token: mdToken) -> Option<Vec<u8>> {
    let mut rid = rid_from_token(token);
    let typ = type_from_token(token);

    if rid > 0x3FFFFFF {
        return None;
    }

    rid = rid << 2;

    // TypeDef is encoded with low bits 00
    // TypeRef is encoded with low bits 01
    // TypeSpec is encoded with low bits 10
    // BaseType is encoded with low bit 11
    if typ == ENCODE_TOKEN[1] {
        // make the last two bits 01
        rid |= 0x1;
    } else if typ == ENCODE_TOKEN[2] {
        // make last two bits 0
        rid |= 0x2;
    } else if typ == ENCODE_TOKEN[3] {
        rid |= 0x3;
    }

    compress_data(rid)
}

/// Given an uncompressed unsigned integer (int), Store it in a compressed format.
/// Based on CorSigCompressData: https://github.com/dotnet/runtime/blob/01b7e73cd378145264a7cb7a09365b41ed42b240/src/coreclr/inc/cor.h#L2111
pub fn compress_data(int: ULONG) -> Option<Vec<u8>> {
    if int <= 0x7F {
        let mut buffer = Vec::with_capacity(1);
        buffer.push(int as BYTE);
        return Some(buffer);
    }

    if int <= 0x3FFF {
        let mut buffer = Vec::with_capacity(2);
        buffer.push(((int >> 8) | 0x80) as BYTE);
        buffer.push((int & 0xff) as BYTE);
        return Some(buffer);
    }

    if int <= 0x1FFFFFFF {
        let mut buffer = Vec::with_capacity(4);
        buffer.push(((int >> 24) | 0xC0) as BYTE);
        buffer.push(((int >> 16) & 0xff) as BYTE);
        buffer.push(((int >> 8) & 0xff) as BYTE);
        buffer.push((int & 0xff) as BYTE);
        return Some(buffer);
    }

    None
}

/// https://github.com/dotnet/runtime/blob/01b7e73cd378145264a7cb7a09365b41ed42b240/src/coreclr/inc/cor.h#L1817
pub fn uncompress_data(data: &[u8]) -> Option<(ULONG, usize)> {
    if data.is_empty() {
        None
    } else if data[0] & 0x80 == 0x00 {
        // 0??? ????
        Some((data[0] as ULONG, 1 as usize))
    } else if data[0] & 0xC0 == 0x80 {
        // 10?? ????
        if data.len() < 2 {
            None
        } else {
            let mut out = ((data[0] as ULONG) & 0x3f) << 8;
            out |= data[1] as ULONG;
            Some((out, 2 as usize))
        }
    } else if data[0] & 0xE0 == 0xC0 {
        // 110? ????
        if data.len() < 4 {
            None
        } else {
            let mut out = ((data[0] as ULONG) & 0x1f) << 24;
            out |= (data[1] as ULONG) << 16;
            out |= (data[2] as ULONG) << 8;
            out |= data[3] as ULONG;
            Some((out, 4 as usize))
        }
    } else {
        None
    }
}

pub fn uncompress_token(data: &[u8]) -> (mdToken, usize) {
    if let Some((uncompressed_data, len)) = uncompress_data(data) {
        let token_type = ENCODE_TOKEN[(uncompressed_data & 0x3) as usize];
        let token = token_from_rid(uncompressed_data >> 2, token_type);
        (token, len)
    } else {
        (mdTokenNil, 0)
    }
}
