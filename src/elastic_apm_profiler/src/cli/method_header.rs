#![allow(non_upper_case_globals)]

/**
Copyright 2019 Camden Reslink
MIT License
https://github.com/camdenreslink/clr-profiler

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute,
sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
use crate::cli::{check_flag, il_u32};
use crate::error::Error;
use crate::ffi::mdTokenNil;

bitflags! {
    pub struct MethodHeaderFlags: u8 {
        const CorILMethod_FatFormat = 0x3;
        const CorILMethod_TinyFormat = 0x2;
        const CorILMethod_MoreSects = 0x8;
        const CorILMethod_InitLocals = 0x10;
        const CorILMethod_FormatShift = 3;
        const CorILMethod_FormatMask = ((1 << MethodHeaderFlags::CorILMethod_FormatShift.bits()) - 1);
        const CorILMethod_SmallFormat = 0x0000;
        const CorILMethod_TinyFormat1 = 0x0006;
    }
}
#[derive(Debug)]
pub struct FatMethodHeader {
    pub more_sects: bool,
    pub init_locals: bool,
    pub max_stack: u16,
    pub code_size: u32,
    pub local_var_sig_tok: u32,
}
impl FatMethodHeader {
    pub const SIZE: u8 = 12;
}
#[derive(Debug)]
pub struct TinyMethodHeader {
    pub code_size: u8,
}
#[derive(Debug)]
pub enum MethodHeader {
    Fat(FatMethodHeader),
    Tiny(TinyMethodHeader),
}
impl MethodHeader {
    pub fn tiny(code_size: u8) -> MethodHeader {
        MethodHeader::Tiny(TinyMethodHeader { code_size })
    }

    pub fn from_bytes(method_il: &[u8]) -> Result<Self, Error> {
        let header_flags = method_il[0];
        if Self::is_tiny(header_flags) {
            // In a tiny header, the first 6 bits encode the code size
            //let code_size = method_il[0] >> 2;
            let code_size = method_il[0] >> (MethodHeaderFlags::CorILMethod_FormatShift.bits() - 1);
            Ok(MethodHeader::Tiny(TinyMethodHeader { code_size }))
        } else if Self::is_fat(header_flags) {
            let more_sects = Self::more_sects(header_flags);
            let init_locals = Self::init_locals(header_flags);
            let max_stack = u16::from_le_bytes([method_il[2], method_il[3]]);
            let code_size = il_u32(method_il, 4)?;
            let local_var_sig_tok = il_u32(method_il, 8)?;
            Ok(MethodHeader::Fat(FatMethodHeader {
                more_sects,
                init_locals,
                max_stack,
                code_size,
                local_var_sig_tok,
            }))
        } else {
            Err(Error::InvalidMethodHeader)
        }
    }

    pub fn into_bytes(&self) -> Vec<u8> {
        let mut bytes = Vec::new();
        match &self {
            MethodHeader::Fat(header) => {
                let mut flags = MethodHeaderFlags::CorILMethod_FatFormat.bits();
                if header.more_sects {
                    flags |= MethodHeaderFlags::CorILMethod_MoreSects.bits();
                }
                if header.init_locals {
                    flags |= MethodHeaderFlags::CorILMethod_InitLocals.bits();
                }
                bytes.push(flags);
                bytes.push(FatMethodHeader::SIZE.reverse_bits());
                bytes.extend_from_slice(&header.max_stack.to_le_bytes());
                bytes.extend_from_slice(&header.code_size.to_le_bytes());
                bytes.extend_from_slice(&header.local_var_sig_tok.to_le_bytes());
            }
            MethodHeader::Tiny(header) => {
                let byte = header.code_size << 2 | MethodHeaderFlags::CorILMethod_TinyFormat.bits();
                bytes.push(byte);
            }
        }
        bytes
    }

    /// Instructions start and end
    pub fn instructions(&self) -> (usize, usize) {
        match self {
            MethodHeader::Fat(header) => (12, (12 + header.code_size - 1) as usize),
            MethodHeader::Tiny(header) => (1, header.code_size as usize),
        }
    }

    pub fn local_var_sig_tok(&self) -> u32 {
        match self {
            MethodHeader::Fat(header) => header.local_var_sig_tok,
            MethodHeader::Tiny(_) => mdTokenNil,
        }
    }

    pub fn max_stack(&self) -> u16 {
        match self {
            MethodHeader::Fat(header) => header.max_stack,
            MethodHeader::Tiny(_) => 8,
        }
    }

    pub fn code_size(&self) -> u32 {
        match self {
            MethodHeader::Fat(header) => header.code_size,
            MethodHeader::Tiny(header) => header.code_size as u32,
        }
    }

    fn more_sects(method_header_flags: u8) -> bool {
        check_flag(
            method_header_flags,
            MethodHeaderFlags::CorILMethod_MoreSects.bits(),
        )
    }
    fn init_locals(method_header_flags: u8) -> bool {
        check_flag(
            method_header_flags,
            MethodHeaderFlags::CorILMethod_InitLocals.bits(),
        )
    }

    fn is_tiny(method_header_flags: u8) -> bool {
        // Check only the 2 least significant bits
        (method_header_flags & 0b00000011) == MethodHeaderFlags::CorILMethod_TinyFormat.bits()
    }

    fn is_fat(method_header_flags: u8) -> bool {
        // Check only the 2 least significant bits
        (method_header_flags & 0b00000011) == MethodHeaderFlags::CorILMethod_FatFormat.bits()
    }
}
