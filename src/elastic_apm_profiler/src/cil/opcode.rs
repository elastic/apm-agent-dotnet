// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
//
// Copyright 2019 Camden Reslink
// MIT License
// https://github.com/camdenreslink/clr-profiler
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
use crate::error::Error;

#[derive(Debug, Eq, PartialEq, Copy, Clone)]
pub enum StackBehaviorPop {
    Pop0,
    Pop1,
    VarPop,
    PopI,
    Pop1Pop1,
    PopIPopI,
    PopIPopI8,
    PopIPopR4,
    PopIPopR8,
    PopRef,
    PopRefPop1,
    PopIPop1,
    PopRefPopI,
    PopRefPopIPopI,
    PopRefPopIPopI8,
    PopRefPopIPopR4,
    PopRefPopIPopR8,
    PopRefPopIPopRef,
    PopRefPopIPop1,
    PopIPopIPopI,
}
#[derive(Debug, Eq, PartialEq, Copy, Clone)]
pub enum StackBehaviorPush {
    Push0,
    Push1,
    PushI,
    PushRef,
    PushI8,
    PushR4,
    PushR8,
    Push1Push1,
    VarPush,
}
impl StackBehaviorPush {
    /// the size on the stack
    pub fn size(&self) -> usize {
        match self {
            Push0 => 0,
            Push1 => 1,
            PushI => 1,
            PushRef => 1,
            PushI8 => 1,
            PushR4 => 1,
            PushR8 => 1,
            Push1Push1 => 2,
            VarPush => 1,
        }
    }
}

#[derive(Debug, Eq, PartialEq, Copy, Clone)]
pub enum OperandParams {
    InlineNone,
    ShortInlineVar,
    InlineVar,
    ShortInlineI,
    InlineI,
    InlineI8,
    ShortInlineR,
    InlineR,
    InlineMethod,
    InlineSig,
    ShortInlineBrTarget,
    InlineBrTarget,
    InlineSwitch,
    InlineType,
    InlineString,
    InlineField,
    InlineTok,
}
#[derive(Debug, Eq, PartialEq, Copy, Clone)]
pub enum OpcodeKind {
    Primitive,
    Macro,
    ObjModel,
    Internal,
    Prefix,
}
#[derive(Debug, Eq, PartialEq, Copy, Clone)]
pub enum ControlFlow {
    Next,
    Break,
    Return,
    Branch,
    CondBranch,
    Call,
    Throw,
    Meta,
}
#[derive(Debug, Eq, PartialEq, Copy, Clone)]
pub struct Opcode {
    pub name: &'static str,
    pub stack_behavior_pop: StackBehaviorPop,
    pub stack_behavior_push: StackBehaviorPush,
    pub operand_params: OperandParams,
    pub opcode_kind: OpcodeKind,
    pub len: u8,
    pub byte_1: u8,
    pub byte_2: u8,
    pub control_flow: ControlFlow,
}

impl Opcode {
    pub const fn new(
        name: &'static str,
        stack_behavior_pop: StackBehaviorPop,
        stack_behavior_push: StackBehaviorPush,
        operand_params: OperandParams,
        opcode_kind: OpcodeKind,
        len: u8,
        byte_1: u8,
        byte_2: u8,
        control_flow: ControlFlow,
    ) -> Self {
        Opcode {
            name,
            stack_behavior_pop,
            stack_behavior_push,
            operand_params,
            opcode_kind,
            len,
            byte_1,
            byte_2,
            control_flow,
        }
    }
    //noinspection RsNonExhaustiveMatch
    pub fn from_byte(byte: u8) -> Self {
        match byte {
            0x00 => NOP,
            0x01 => BREAK,
            0x02 => LDARG_0,
            0x03 => LDARG_1,
            0x04 => LDARG_2,
            0x05 => LDARG_3,
            0x06 => LDLOC_0,
            0x07 => LDLOC_1,
            0x08 => LDLOC_2,
            0x09 => LDLOC_3,
            0x0A => STLOC_0,
            0x0B => STLOC_1,
            0x0C => STLOC_2,
            0x0D => STLOC_3,
            0x0E => LDARG_S,
            0x0F => LDARGA_S,
            0x10 => STARG_S,
            0x11 => LDLOC_S,
            0x12 => LDLOCA_S,
            0x13 => STLOC_S,
            0x14 => LDNULL,
            0x15 => LDC_I4_M1,
            0x16 => LDC_I4_0,
            0x17 => LDC_I4_1,
            0x18 => LDC_I4_2,
            0x19 => LDC_I4_3,
            0x1A => LDC_I4_4,
            0x1B => LDC_I4_5,
            0x1C => LDC_I4_6,
            0x1D => LDC_I4_7,
            0x1E => LDC_I4_8,
            0x1F => LDC_I4_S,
            0x20 => LDC_I4,
            0x21 => LDC_I8,
            0x22 => LDC_R4,
            0x23 => LDC_R8,
            0x24 => UNUSED49,
            0x25 => DUP,
            0x26 => POP,
            0x27 => JMP,
            0x28 => CALL,
            0x29 => CALLI,
            0x2A => RET,
            0x2B => BR_S,
            0x2C => BRFALSE_S,
            0x2D => BRTRUE_S,
            0x2E => BEQ_S,
            0x2F => BGE_S,
            0x30 => BGT_S,
            0x31 => BLE_S,
            0x32 => BLT_S,
            0x33 => BNE_UN_S,
            0x34 => BGE_UN_S,
            0x35 => BGT_UN_S,
            0x36 => BLE_UN_S,
            0x37 => BLT_UN_S,
            0x38 => BR,
            0x39 => BRFALSE,
            0x3A => BRTRUE,
            0x3B => BEQ,
            0x3C => BGE,
            0x3D => BGT,
            0x3E => BLE,
            0x3F => BLT,
            0x40 => BNE_UN,
            0x41 => BGE_UN,
            0x42 => BGT_UN,
            0x43 => BLE_UN,
            0x44 => BLT_UN,
            0x45 => SWITCH,
            0x46 => LDIND_I1,
            0x47 => LDIND_U1,
            0x48 => LDIND_I2,
            0x49 => LDIND_U2,
            0x4A => LDIND_I4,
            0x4B => LDIND_U4,
            0x4C => LDIND_I8,
            0x4D => LDIND_I,
            0x4E => LDIND_R4,
            0x4F => LDIND_R8,
            0x50 => LDIND_REF,
            0x51 => STIND_REF,
            0x52 => STIND_I1,
            0x53 => STIND_I2,
            0x54 => STIND_I4,
            0x55 => STIND_I8,
            0x56 => STIND_R4,
            0x57 => STIND_R8,
            0x58 => ADD,
            0x59 => SUB,
            0x5A => MUL,
            0x5B => DIV,
            0x5C => DIV_UN,
            0x5D => REM,
            0x5E => REM_UN,
            0x5F => AND,
            0x60 => OR,
            0x61 => XOR,
            0x62 => SHL,
            0x63 => SHR,
            0x64 => SHR_UN,
            0x65 => NEG,
            0x66 => NOT,
            0x67 => CONV_I1,
            0x68 => CONV_I2,
            0x69 => CONV_I4,
            0x6A => CONV_I8,
            0x6B => CONV_R4,
            0x6C => CONV_R8,
            0x6D => CONV_U4,
            0x6E => CONV_U8,
            0x6F => CALLVIRT,
            0x70 => CPOBJ,
            0x71 => LDOBJ,
            0x72 => LDSTR,
            0x73 => NEWOBJ,
            0x74 => CASTCLASS,
            0x75 => ISINST,
            0x76 => CONV_R_UN,
            0x77 => UNUSED58,
            0x78 => UNUSED1,
            0x79 => UNBOX,
            0x7A => THROW,
            0x7B => LDFLD,
            0x7C => LDFLDA,
            0x7D => STFLD,
            0x7E => LDSFLD,
            0x7F => LDSFLDA,
            0x80 => STSFLD,
            0x81 => STOBJ,
            0x82 => CONV_OVF_I1_UN,
            0x83 => CONV_OVF_I2_UN,
            0x84 => CONV_OVF_I4_UN,
            0x85 => CONV_OVF_I8_UN,
            0x86 => CONV_OVF_U1_UN,
            0x87 => CONV_OVF_U2_UN,
            0x88 => CONV_OVF_U4_UN,
            0x89 => CONV_OVF_U8_UN,
            0x8A => CONV_OVF_I_UN,
            0x8B => CONV_OVF_U_UN,
            0x8C => BOX,
            0x8D => NEWARR,
            0x8E => LDLEN,
            0x8F => LDELEMA,
            0x90 => LDELEM_I1,
            0x91 => LDELEM_U1,
            0x92 => LDELEM_I2,
            0x93 => LDELEM_U2,
            0x94 => LDELEM_I4,
            0x95 => LDELEM_U4,
            0x96 => LDELEM_I8,
            0x97 => LDELEM_I,
            0x98 => LDELEM_R4,
            0x99 => LDELEM_R8,
            0x9A => LDELEM_REF,
            0x9B => STELEM_I,
            0x9C => STELEM_I1,
            0x9D => STELEM_I2,
            0x9E => STELEM_I4,
            0x9F => STELEM_I8,
            0xA0 => STELEM_R4,
            0xA1 => STELEM_R8,
            0xA2 => STELEM_REF,
            0xA3 => LDELEM,
            0xA4 => STELEM,
            0xA5 => UNBOX_ANY,
            0xA6 => UNUSED5,
            0xA7 => UNUSED6,
            0xA8 => UNUSED7,
            0xA9 => UNUSED8,
            0xAA => UNUSED9,
            0xAB => UNUSED10,
            0xAC => UNUSED11,
            0xAD => UNUSED12,
            0xAE => UNUSED13,
            0xAF => UNUSED14,
            0xB0 => UNUSED15,
            0xB1 => UNUSED16,
            0xB2 => UNUSED17,
            0xB3 => CONV_OVF_I1,
            0xB4 => CONV_OVF_U1,
            0xB5 => CONV_OVF_I2,
            0xB6 => CONV_OVF_U2,
            0xB7 => CONV_OVF_I4,
            0xB8 => CONV_OVF_U4,
            0xB9 => CONV_OVF_I8,
            0xBA => CONV_OVF_U8,
            0xBB => UNUSED50,
            0xBC => UNUSED18,
            0xBD => UNUSED19,
            0xBE => UNUSED20,
            0xBF => UNUSED21,
            0xC0 => UNUSED22,
            0xC1 => UNUSED23,
            0xC2 => REFANYVAL,
            0xC3 => CKFINITE,
            0xC4 => UNUSED24,
            0xC5 => UNUSED25,
            0xC6 => MKREFANY,
            0xC7 => UNUSED59,
            0xC8 => UNUSED60,
            0xC9 => UNUSED61,
            0xCA => UNUSED62,
            0xCB => UNUSED63,
            0xCC => UNUSED64,
            0xCD => UNUSED65,
            0xCE => UNUSED66,
            0xCF => UNUSED67,
            0xD0 => LDTOKEN,
            0xD1 => CONV_U2,
            0xD2 => CONV_U1,
            0xD3 => CONV_I,
            0xD4 => CONV_OVF_I,
            0xD5 => CONV_OVF_U,
            0xD6 => ADD_OVF,
            0xD7 => ADD_OVF_UN,
            0xD8 => MUL_OVF,
            0xD9 => MUL_OVF_UN,
            0xDA => SUB_OVF,
            0xDB => SUB_OVF_UN,
            0xDC => ENDFINALLY,
            0xDD => LEAVE,
            0xDE => LEAVE_S,
            0xDF => STIND_I,
            0xE0 => CONV_U,
            0xE1 => UNUSED26,
            0xE2 => UNUSED27,
            0xE3 => UNUSED28,
            0xE4 => UNUSED29,
            0xE5 => UNUSED30,
            0xE6 => UNUSED31,
            0xE7 => UNUSED32,
            0xE8 => UNUSED33,
            0xE9 => UNUSED34,
            0xEA => UNUSED35,
            0xEB => UNUSED36,
            0xEC => UNUSED37,
            0xED => UNUSED38,
            0xEE => UNUSED39,
            0xEF => UNUSED40,
            0xF0 => UNUSED41,
            0xF1 => UNUSED42,
            0xF2 => UNUSED43,
            0xF3 => UNUSED44,
            0xF4 => UNUSED45,
            0xF5 => UNUSED46,
            0xF6 => UNUSED47,
            0xF7 => UNUSED48,
            0xF8 => PREFIX7,
            0xF9 => PREFIX6,
            0xFA => PREFIX5,
            0xFB => PREFIX4,
            0xFC => PREFIX3,
            0xFD => PREFIX2,
            0xFE => PREFIX1,
            0xFF => PREFIXREF,
        }
    }
    pub fn from_byte_pair(pair: (u8, u8)) -> Result<Self, Error> {
        // #define STP1 0xFE
        // #define REFPRE 0xFF
        match pair {
            (0xFE, 0x00) => Ok(ARGLIST),
            (0xFE, 0x01) => Ok(CEQ),
            (0xFE, 0x02) => Ok(CGT),
            (0xFE, 0x03) => Ok(CGT_UN),
            (0xFE, 0x04) => Ok(CLT),
            (0xFE, 0x05) => Ok(CLT_UN),
            (0xFE, 0x06) => Ok(LDFTN),
            (0xFE, 0x07) => Ok(LDVIRTFTN),
            (0xFE, 0x08) => Ok(UNUSED56),
            (0xFE, 0x09) => Ok(LDARG),
            (0xFE, 0x0A) => Ok(LDARGA),
            (0xFE, 0x0B) => Ok(STARG),
            (0xFE, 0x0C) => Ok(LDLOC),
            (0xFE, 0x0D) => Ok(LDLOCA),
            (0xFE, 0x0E) => Ok(STLOC),
            (0xFE, 0x0F) => Ok(LOCALLOC),
            (0xFE, 0x10) => Ok(UNUSED57),
            (0xFE, 0x11) => Ok(ENDFILTER),
            (0xFE, 0x12) => Ok(UNALIGNED),
            (0xFE, 0x13) => Ok(VOLATILE),
            (0xFE, 0x14) => Ok(TAILCALL),
            (0xFE, 0x15) => Ok(INITOBJ),
            (0xFE, 0x16) => Ok(CONSTRAINED),
            (0xFE, 0x17) => Ok(CPBLK),
            (0xFE, 0x18) => Ok(INITBLK),
            (0xFE, 0x19) => Ok(UNUSED69),
            (0xFE, 0x1A) => Ok(RETHROW),
            (0xFE, 0x1B) => Ok(UNUSED51),
            (0xFE, 0x1C) => Ok(SIZEOF),
            (0xFE, 0x1D) => Ok(REFANYTYPE),
            (0xFE, 0x1E) => Ok(READONLY),
            (0xFE, 0x1F) => Ok(UNUSED53),
            (0xFE, 0x20) => Ok(UNUSED54),
            (0xFE, 0x21) => Ok(UNUSED55),
            (0xFE, 0x22) => Ok(UNUSED70),
            _ => Err(Error::InvalidCilOpcode),
        }
    }

    pub fn short_to_long_form(opcode: Opcode) -> Opcode {
        match opcode {
            BRFALSE_S => BRFALSE,
            BRTRUE_S => BRTRUE,
            BEQ_S => BEQ,
            BGE_S => BGE,
            BGT_S => BGT,
            BLE_S => BLE,
            BLT_S => BLT,
            BR_S => BR,
            BGE_UN_S => BGE_UN,
            BGT_UN_S => BGT_UN,
            BLE_UN_S => BLE_UN,
            BLT_UN_S => BLT_UN,
            BNE_UN_S => BNE_UN,
            _ => opcode,
        }
    }
}

use self::{
    ControlFlow::*, OpcodeKind::*, OperandParams::*, StackBehaviorPop::*, StackBehaviorPush::*,
};

pub const NOP: Opcode = Opcode::new(
    "nop", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0x00, Next,
);
pub const BREAK: Opcode = Opcode::new(
    "break", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0x01, Break,
);
pub const LDARG_0: Opcode = Opcode::new(
    "ldarg.0", Pop0, Push1, InlineNone, Macro, 1, 0xFF, 0x02, Next,
);
pub const LDARG_1: Opcode = Opcode::new(
    "ldarg.1", Pop0, Push1, InlineNone, Macro, 1, 0xFF, 0x03, Next,
);
pub const LDARG_2: Opcode = Opcode::new(
    "ldarg.2", Pop0, Push1, InlineNone, Macro, 1, 0xFF, 0x04, Next,
);
pub const LDARG_3: Opcode = Opcode::new(
    "ldarg.3", Pop0, Push1, InlineNone, Macro, 1, 0xFF, 0x05, Next,
);
pub const LDLOC_0: Opcode = Opcode::new(
    "ldloc.0", Pop0, Push1, InlineNone, Macro, 1, 0xFF, 0x06, Next,
);
pub const LDLOC_1: Opcode = Opcode::new(
    "ldloc.1", Pop0, Push1, InlineNone, Macro, 1, 0xFF, 0x07, Next,
);
pub const LDLOC_2: Opcode = Opcode::new(
    "ldloc.2", Pop0, Push1, InlineNone, Macro, 1, 0xFF, 0x08, Next,
);
pub const LDLOC_3: Opcode = Opcode::new(
    "ldloc.3", Pop0, Push1, InlineNone, Macro, 1, 0xFF, 0x09, Next,
);
pub const STLOC_0: Opcode = Opcode::new(
    "stloc.0", Pop1, Push0, InlineNone, Macro, 1, 0xFF, 0x0A, Next,
);
pub const STLOC_1: Opcode = Opcode::new(
    "stloc.1", Pop1, Push0, InlineNone, Macro, 1, 0xFF, 0x0B, Next,
);
pub const STLOC_2: Opcode = Opcode::new(
    "stloc.2", Pop1, Push0, InlineNone, Macro, 1, 0xFF, 0x0C, Next,
);
pub const STLOC_3: Opcode = Opcode::new(
    "stloc.3", Pop1, Push0, InlineNone, Macro, 1, 0xFF, 0x0D, Next,
);
pub const LDARG_S: Opcode = Opcode::new(
    "ldarg.s",
    Pop0,
    Push1,
    ShortInlineVar,
    Macro,
    1,
    0xFF,
    0x0E,
    Next,
);
pub const LDARGA_S: Opcode = Opcode::new(
    "ldarga.s",
    Pop0,
    PushI,
    ShortInlineVar,
    Macro,
    1,
    0xFF,
    0x0F,
    Next,
);
pub const STARG_S: Opcode = Opcode::new(
    "starg.s",
    Pop1,
    Push0,
    ShortInlineVar,
    Macro,
    1,
    0xFF,
    0x10,
    Next,
);
pub const LDLOC_S: Opcode = Opcode::new(
    "ldloc.s",
    Pop0,
    Push1,
    ShortInlineVar,
    Macro,
    1,
    0xFF,
    0x11,
    Next,
);
pub const LDLOCA_S: Opcode = Opcode::new(
    "ldloca.s",
    Pop0,
    PushI,
    ShortInlineVar,
    Macro,
    1,
    0xFF,
    0x12,
    Next,
);
pub const STLOC_S: Opcode = Opcode::new(
    "stloc.s",
    Pop1,
    Push0,
    ShortInlineVar,
    Macro,
    1,
    0xFF,
    0x13,
    Next,
);
pub const LDNULL: Opcode = Opcode::new(
    "ldnull", Pop0, PushRef, InlineNone, Primitive, 1, 0xFF, 0x14, Next,
);
pub const LDC_I4_M1: Opcode = Opcode::new(
    "ldc.i4.m1",
    Pop0,
    PushI,
    InlineNone,
    Macro,
    1,
    0xFF,
    0x15,
    Next,
);
pub const LDC_I4_0: Opcode = Opcode::new(
    "ldc.i4.0", Pop0, PushI, InlineNone, Macro, 1, 0xFF, 0x16, Next,
);
pub const LDC_I4_1: Opcode = Opcode::new(
    "ldc.i4.1", Pop0, PushI, InlineNone, Macro, 1, 0xFF, 0x17, Next,
);
pub const LDC_I4_2: Opcode = Opcode::new(
    "ldc.i4.2", Pop0, PushI, InlineNone, Macro, 1, 0xFF, 0x18, Next,
);
pub const LDC_I4_3: Opcode = Opcode::new(
    "ldc.i4.3", Pop0, PushI, InlineNone, Macro, 1, 0xFF, 0x19, Next,
);
pub const LDC_I4_4: Opcode = Opcode::new(
    "ldc.i4.4", Pop0, PushI, InlineNone, Macro, 1, 0xFF, 0x1A, Next,
);
pub const LDC_I4_5: Opcode = Opcode::new(
    "ldc.i4.5", Pop0, PushI, InlineNone, Macro, 1, 0xFF, 0x1B, Next,
);
pub const LDC_I4_6: Opcode = Opcode::new(
    "ldc.i4.6", Pop0, PushI, InlineNone, Macro, 1, 0xFF, 0x1C, Next,
);
pub const LDC_I4_7: Opcode = Opcode::new(
    "ldc.i4.7", Pop0, PushI, InlineNone, Macro, 1, 0xFF, 0x1D, Next,
);
pub const LDC_I4_8: Opcode = Opcode::new(
    "ldc.i4.8", Pop0, PushI, InlineNone, Macro, 1, 0xFF, 0x1E, Next,
);
pub const LDC_I4_S: Opcode = Opcode::new(
    "ldc.i4.s",
    Pop0,
    PushI,
    ShortInlineI,
    Macro,
    1,
    0xFF,
    0x1F,
    Next,
);
pub const LDC_I4: Opcode = Opcode::new(
    "ldc.i4", Pop0, PushI, InlineI, Primitive, 1, 0xFF, 0x20, Next,
);
pub const LDC_I8: Opcode = Opcode::new(
    "ldc.i8", Pop0, PushI8, InlineI8, Primitive, 1, 0xFF, 0x21, Next,
);
pub const LDC_R4: Opcode = Opcode::new(
    "ldc.r4",
    Pop0,
    PushR4,
    ShortInlineR,
    Primitive,
    1,
    0xFF,
    0x22,
    Next,
);
pub const LDC_R8: Opcode = Opcode::new(
    "ldc.r8", Pop0, PushR8, InlineR, Primitive, 1, 0xFF, 0x23, Next,
);
pub const UNUSED49: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0x24, Next,
);
pub const DUP: Opcode = Opcode::new(
    "dup", Pop1, Push1Push1, InlineNone, Primitive, 1, 0xFF, 0x25, Next,
);
pub const POP: Opcode = Opcode::new(
    "pop", Pop1, Push0, InlineNone, Primitive, 1, 0xFF, 0x26, Next,
);
pub const JMP: Opcode = Opcode::new(
    "jmp",
    Pop0,
    Push0,
    InlineMethod,
    Primitive,
    1,
    0xFF,
    0x27,
    Call,
);
pub const CALL: Opcode = Opcode::new(
    "call",
    VarPop,
    VarPush,
    InlineMethod,
    Primitive,
    1,
    0xFF,
    0x28,
    Call,
);
pub const CALLI: Opcode = Opcode::new(
    "calli", VarPop, VarPush, InlineSig, Primitive, 1, 0xFF, 0x29, Call,
);
pub const RET: Opcode = Opcode::new(
    "ret", VarPop, Push0, InlineNone, Primitive, 1, 0xFF, 0x2A, Return,
);
pub const BR_S: Opcode = Opcode::new(
    "br.s",
    Pop0,
    Push0,
    ShortInlineBrTarget,
    Macro,
    1,
    0xFF,
    0x2B,
    Branch,
);
pub const BRFALSE_S: Opcode = Opcode::new(
    "brfalse.s",
    PopI,
    Push0,
    ShortInlineBrTarget,
    Macro,
    1,
    0xFF,
    0x2C,
    CondBranch,
);
pub const BRTRUE_S: Opcode = Opcode::new(
    "brtrue.s",
    PopI,
    Push0,
    ShortInlineBrTarget,
    Macro,
    1,
    0xFF,
    0x2D,
    CondBranch,
);
pub const BEQ_S: Opcode = Opcode::new(
    "beq.s",
    Pop1Pop1,
    Push0,
    ShortInlineBrTarget,
    Macro,
    1,
    0xFF,
    0x2E,
    CondBranch,
);
pub const BGE_S: Opcode = Opcode::new(
    "bge.s",
    Pop1Pop1,
    Push0,
    ShortInlineBrTarget,
    Macro,
    1,
    0xFF,
    0x2F,
    CondBranch,
);
pub const BGT_S: Opcode = Opcode::new(
    "bgt.s",
    Pop1Pop1,
    Push0,
    ShortInlineBrTarget,
    Macro,
    1,
    0xFF,
    0x30,
    CondBranch,
);
pub const BLE_S: Opcode = Opcode::new(
    "ble.s",
    Pop1Pop1,
    Push0,
    ShortInlineBrTarget,
    Macro,
    1,
    0xFF,
    0x31,
    CondBranch,
);
pub const BLT_S: Opcode = Opcode::new(
    "blt.s",
    Pop1Pop1,
    Push0,
    ShortInlineBrTarget,
    Macro,
    1,
    0xFF,
    0x32,
    CondBranch,
);
pub const BNE_UN_S: Opcode = Opcode::new(
    "bne.un.s",
    Pop1Pop1,
    Push0,
    ShortInlineBrTarget,
    Macro,
    1,
    0xFF,
    0x33,
    CondBranch,
);
pub const BGE_UN_S: Opcode = Opcode::new(
    "bge.un.s",
    Pop1Pop1,
    Push0,
    ShortInlineBrTarget,
    Macro,
    1,
    0xFF,
    0x34,
    CondBranch,
);
pub const BGT_UN_S: Opcode = Opcode::new(
    "bgt.un.s",
    Pop1Pop1,
    Push0,
    ShortInlineBrTarget,
    Macro,
    1,
    0xFF,
    0x35,
    CondBranch,
);
pub const BLE_UN_S: Opcode = Opcode::new(
    "ble.un.s",
    Pop1Pop1,
    Push0,
    ShortInlineBrTarget,
    Macro,
    1,
    0xFF,
    0x36,
    CondBranch,
);
pub const BLT_UN_S: Opcode = Opcode::new(
    "blt.un.s",
    Pop1Pop1,
    Push0,
    ShortInlineBrTarget,
    Macro,
    1,
    0xFF,
    0x37,
    CondBranch,
);
pub const BR: Opcode = Opcode::new(
    "br",
    Pop0,
    Push0,
    InlineBrTarget,
    Primitive,
    1,
    0xFF,
    0x38,
    Branch,
);
pub const BRFALSE: Opcode = Opcode::new(
    "brfalse",
    PopI,
    Push0,
    InlineBrTarget,
    Primitive,
    1,
    0xFF,
    0x39,
    CondBranch,
);
pub const BRTRUE: Opcode = Opcode::new(
    "brtrue",
    PopI,
    Push0,
    InlineBrTarget,
    Primitive,
    1,
    0xFF,
    0x3A,
    CondBranch,
);
pub const BEQ: Opcode = Opcode::new(
    "beq",
    Pop1Pop1,
    Push0,
    InlineBrTarget,
    Macro,
    1,
    0xFF,
    0x3B,
    CondBranch,
);
pub const BGE: Opcode = Opcode::new(
    "bge",
    Pop1Pop1,
    Push0,
    InlineBrTarget,
    Macro,
    1,
    0xFF,
    0x3C,
    CondBranch,
);
pub const BGT: Opcode = Opcode::new(
    "bgt",
    Pop1Pop1,
    Push0,
    InlineBrTarget,
    Macro,
    1,
    0xFF,
    0x3D,
    CondBranch,
);
pub const BLE: Opcode = Opcode::new(
    "ble",
    Pop1Pop1,
    Push0,
    InlineBrTarget,
    Macro,
    1,
    0xFF,
    0x3E,
    CondBranch,
);
pub const BLT: Opcode = Opcode::new(
    "blt",
    Pop1Pop1,
    Push0,
    InlineBrTarget,
    Macro,
    1,
    0xFF,
    0x3F,
    CondBranch,
);
pub const BNE_UN: Opcode = Opcode::new(
    "bne.un",
    Pop1Pop1,
    Push0,
    InlineBrTarget,
    Macro,
    1,
    0xFF,
    0x40,
    CondBranch,
);
pub const BGE_UN: Opcode = Opcode::new(
    "bge.un",
    Pop1Pop1,
    Push0,
    InlineBrTarget,
    Macro,
    1,
    0xFF,
    0x41,
    CondBranch,
);
pub const BGT_UN: Opcode = Opcode::new(
    "bgt.un",
    Pop1Pop1,
    Push0,
    InlineBrTarget,
    Macro,
    1,
    0xFF,
    0x42,
    CondBranch,
);
pub const BLE_UN: Opcode = Opcode::new(
    "ble.un",
    Pop1Pop1,
    Push0,
    InlineBrTarget,
    Macro,
    1,
    0xFF,
    0x43,
    CondBranch,
);
pub const BLT_UN: Opcode = Opcode::new(
    "blt.un",
    Pop1Pop1,
    Push0,
    InlineBrTarget,
    Macro,
    1,
    0xFF,
    0x44,
    CondBranch,
);
pub const SWITCH: Opcode = Opcode::new(
    "switch",
    PopI,
    Push0,
    InlineSwitch,
    Primitive,
    1,
    0xFF,
    0x45,
    CondBranch,
);
pub const LDIND_I1: Opcode = Opcode::new(
    "ldind.i1", PopI, PushI, InlineNone, Primitive, 1, 0xFF, 0x46, Next,
);
pub const LDIND_U1: Opcode = Opcode::new(
    "ldind.u1", PopI, PushI, InlineNone, Primitive, 1, 0xFF, 0x47, Next,
);
pub const LDIND_I2: Opcode = Opcode::new(
    "ldind.i2", PopI, PushI, InlineNone, Primitive, 1, 0xFF, 0x48, Next,
);
pub const LDIND_U2: Opcode = Opcode::new(
    "ldind.u2", PopI, PushI, InlineNone, Primitive, 1, 0xFF, 0x49, Next,
);
pub const LDIND_I4: Opcode = Opcode::new(
    "ldind.i4", PopI, PushI, InlineNone, Primitive, 1, 0xFF, 0x4A, Next,
);
pub const LDIND_U4: Opcode = Opcode::new(
    "ldind.u4", PopI, PushI, InlineNone, Primitive, 1, 0xFF, 0x4B, Next,
);
pub const LDIND_I8: Opcode = Opcode::new(
    "ldind.i8", PopI, PushI8, InlineNone, Primitive, 1, 0xFF, 0x4C, Next,
);
pub const LDIND_I: Opcode = Opcode::new(
    "ldind.i", PopI, PushI, InlineNone, Primitive, 1, 0xFF, 0x4D, Next,
);
pub const LDIND_R4: Opcode = Opcode::new(
    "ldind.r4", PopI, PushR4, InlineNone, Primitive, 1, 0xFF, 0x4E, Next,
);
pub const LDIND_R8: Opcode = Opcode::new(
    "ldind.r8", PopI, PushR8, InlineNone, Primitive, 1, 0xFF, 0x4F, Next,
);
pub const LDIND_REF: Opcode = Opcode::new(
    "ldind.ref",
    PopI,
    PushRef,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0x50,
    Next,
);
pub const STIND_REF: Opcode = Opcode::new(
    "stind.ref",
    PopIPopI,
    Push0,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0x51,
    Next,
);
pub const STIND_I1: Opcode = Opcode::new(
    "stind.i1", PopIPopI, Push0, InlineNone, Primitive, 1, 0xFF, 0x52, Next,
);
pub const STIND_I2: Opcode = Opcode::new(
    "stind.i2", PopIPopI, Push0, InlineNone, Primitive, 1, 0xFF, 0x53, Next,
);
pub const STIND_I4: Opcode = Opcode::new(
    "stind.i4", PopIPopI, Push0, InlineNone, Primitive, 1, 0xFF, 0x54, Next,
);
pub const STIND_I8: Opcode = Opcode::new(
    "stind.i8", PopIPopI8, Push0, InlineNone, Primitive, 1, 0xFF, 0x55, Next,
);
pub const STIND_R4: Opcode = Opcode::new(
    "stind.r4", PopIPopR4, Push0, InlineNone, Primitive, 1, 0xFF, 0x56, Next,
);
pub const STIND_R8: Opcode = Opcode::new(
    "stind.r8", PopIPopR8, Push0, InlineNone, Primitive, 1, 0xFF, 0x57, Next,
);
pub const ADD: Opcode = Opcode::new(
    "add", Pop1Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0x58, Next,
);
pub const SUB: Opcode = Opcode::new(
    "sub", Pop1Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0x59, Next,
);
pub const MUL: Opcode = Opcode::new(
    "mul", Pop1Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0x5A, Next,
);
pub const DIV: Opcode = Opcode::new(
    "div", Pop1Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0x5B, Next,
);
pub const DIV_UN: Opcode = Opcode::new(
    "div.un", Pop1Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0x5C, Next,
);
pub const REM: Opcode = Opcode::new(
    "rem", Pop1Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0x5D, Next,
);
pub const REM_UN: Opcode = Opcode::new(
    "rem.un", Pop1Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0x5E, Next,
);
pub const AND: Opcode = Opcode::new(
    "and", Pop1Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0x5F, Next,
);
pub const OR: Opcode = Opcode::new(
    "or", Pop1Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0x60, Next,
);
pub const XOR: Opcode = Opcode::new(
    "xor", Pop1Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0x61, Next,
);
pub const SHL: Opcode = Opcode::new(
    "shl", Pop1Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0x62, Next,
);
pub const SHR: Opcode = Opcode::new(
    "shr", Pop1Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0x63, Next,
);
pub const SHR_UN: Opcode = Opcode::new(
    "shr.un", Pop1Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0x64, Next,
);
pub const NEG: Opcode = Opcode::new(
    "neg", Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0x65, Next,
);
pub const NOT: Opcode = Opcode::new(
    "not", Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0x66, Next,
);
pub const CONV_I1: Opcode = Opcode::new(
    "conv.i1", Pop1, PushI, InlineNone, Primitive, 1, 0xFF, 0x67, Next,
);
pub const CONV_I2: Opcode = Opcode::new(
    "conv.i2", Pop1, PushI, InlineNone, Primitive, 1, 0xFF, 0x68, Next,
);
pub const CONV_I4: Opcode = Opcode::new(
    "conv.i4", Pop1, PushI, InlineNone, Primitive, 1, 0xFF, 0x69, Next,
);
pub const CONV_I8: Opcode = Opcode::new(
    "conv.i8", Pop1, PushI8, InlineNone, Primitive, 1, 0xFF, 0x6A, Next,
);
pub const CONV_R4: Opcode = Opcode::new(
    "conv.r4", Pop1, PushR4, InlineNone, Primitive, 1, 0xFF, 0x6B, Next,
);
pub const CONV_R8: Opcode = Opcode::new(
    "conv.r8", Pop1, PushR8, InlineNone, Primitive, 1, 0xFF, 0x6C, Next,
);
pub const CONV_U4: Opcode = Opcode::new(
    "conv.u4", Pop1, PushI, InlineNone, Primitive, 1, 0xFF, 0x6D, Next,
);
pub const CONV_U8: Opcode = Opcode::new(
    "conv.u8", Pop1, PushI8, InlineNone, Primitive, 1, 0xFF, 0x6E, Next,
);
pub const CALLVIRT: Opcode = Opcode::new(
    "callvirt",
    VarPop,
    VarPush,
    InlineMethod,
    ObjModel,
    1,
    0xFF,
    0x6F,
    Call,
);
pub const CPOBJ: Opcode = Opcode::new(
    "cpobj", PopIPopI, Push0, InlineType, ObjModel, 1, 0xFF, 0x70, Next,
);
pub const LDOBJ: Opcode = Opcode::new(
    "ldobj", PopI, Push1, InlineType, ObjModel, 1, 0xFF, 0x71, Next,
);
pub const LDSTR: Opcode = Opcode::new(
    "ldstr",
    Pop0,
    PushRef,
    InlineString,
    ObjModel,
    1,
    0xFF,
    0x72,
    Next,
);
pub const NEWOBJ: Opcode = Opcode::new(
    "newobj",
    VarPop,
    PushRef,
    InlineMethod,
    ObjModel,
    1,
    0xFF,
    0x73,
    Call,
);
pub const CASTCLASS: Opcode = Opcode::new(
    "castclass",
    PopRef,
    PushRef,
    InlineType,
    ObjModel,
    1,
    0xFF,
    0x74,
    Next,
);
pub const ISINST: Opcode = Opcode::new(
    "isinst", PopRef, PushI, InlineType, ObjModel, 1, 0xFF, 0x75, Next,
);
pub const CONV_R_UN: Opcode = Opcode::new(
    "conv.r.un",
    Pop1,
    PushR8,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0x76,
    Next,
);
pub const UNUSED58: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0x77, Next,
);
pub const UNUSED1: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0x78, Next,
);
pub const UNBOX: Opcode = Opcode::new(
    "unbox", PopRef, PushI, InlineType, Primitive, 1, 0xFF, 0x79, Next,
);
pub const THROW: Opcode = Opcode::new(
    "throw", PopRef, Push0, InlineNone, ObjModel, 1, 0xFF, 0x7A, Throw,
);
pub const LDFLD: Opcode = Opcode::new(
    "ldfld",
    PopRef,
    Push1,
    InlineField,
    ObjModel,
    1,
    0xFF,
    0x7B,
    Next,
);
pub const LDFLDA: Opcode = Opcode::new(
    "ldflda",
    PopRef,
    PushI,
    InlineField,
    ObjModel,
    1,
    0xFF,
    0x7C,
    Next,
);
pub const STFLD: Opcode = Opcode::new(
    "stfld",
    PopRefPop1,
    Push0,
    InlineField,
    ObjModel,
    1,
    0xFF,
    0x7D,
    Next,
);
pub const LDSFLD: Opcode = Opcode::new(
    "ldsfld",
    Pop0,
    Push1,
    InlineField,
    ObjModel,
    1,
    0xFF,
    0x7E,
    Next,
);
pub const LDSFLDA: Opcode = Opcode::new(
    "ldsflda",
    Pop0,
    PushI,
    InlineField,
    ObjModel,
    1,
    0xFF,
    0x7F,
    Next,
);
pub const STSFLD: Opcode = Opcode::new(
    "stsfld",
    Pop1,
    Push0,
    InlineField,
    ObjModel,
    1,
    0xFF,
    0x80,
    Next,
);
pub const STOBJ: Opcode = Opcode::new(
    "stobj", PopIPop1, Push0, InlineType, Primitive, 1, 0xFF, 0x81, Next,
);
pub const CONV_OVF_I1_UN: Opcode = Opcode::new(
    "conv.ovf.i1.un",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0x82,
    Next,
);
pub const CONV_OVF_I2_UN: Opcode = Opcode::new(
    "conv.ovf.i2.un",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0x83,
    Next,
);
pub const CONV_OVF_I4_UN: Opcode = Opcode::new(
    "conv.ovf.i4.un",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0x84,
    Next,
);
pub const CONV_OVF_I8_UN: Opcode = Opcode::new(
    "conv.ovf.i8.un",
    Pop1,
    PushI8,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0x85,
    Next,
);
pub const CONV_OVF_U1_UN: Opcode = Opcode::new(
    "conv.ovf.u1.un",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0x86,
    Next,
);
pub const CONV_OVF_U2_UN: Opcode = Opcode::new(
    "conv.ovf.u2.un",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0x87,
    Next,
);
pub const CONV_OVF_U4_UN: Opcode = Opcode::new(
    "conv.ovf.u4.un",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0x88,
    Next,
);
pub const CONV_OVF_U8_UN: Opcode = Opcode::new(
    "conv.ovf.u8.un",
    Pop1,
    PushI8,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0x89,
    Next,
);
pub const CONV_OVF_I_UN: Opcode = Opcode::new(
    "conv.ovf.i.un",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0x8A,
    Next,
);
pub const CONV_OVF_U_UN: Opcode = Opcode::new(
    "conv.ovf.u.un",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0x8B,
    Next,
);
pub const BOX: Opcode = Opcode::new(
    "box", Pop1, PushRef, InlineType, Primitive, 1, 0xFF, 0x8C, Next,
);
pub const NEWARR: Opcode = Opcode::new(
    "newarr", PopI, PushRef, InlineType, ObjModel, 1, 0xFF, 0x8D, Next,
);
pub const LDLEN: Opcode = Opcode::new(
    "ldlen", PopRef, PushI, InlineNone, ObjModel, 1, 0xFF, 0x8E, Next,
);
pub const LDELEMA: Opcode = Opcode::new(
    "ldelema", PopRefPopI, PushI, InlineType, ObjModel, 1, 0xFF, 0x8F, Next,
);
pub const LDELEM_I1: Opcode = Opcode::new(
    "ldelem.i1",
    PopRefPopI,
    PushI,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0x90,
    Next,
);
pub const LDELEM_U1: Opcode = Opcode::new(
    "ldelem.u1",
    PopRefPopI,
    PushI,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0x91,
    Next,
);
pub const LDELEM_I2: Opcode = Opcode::new(
    "ldelem.i2",
    PopRefPopI,
    PushI,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0x92,
    Next,
);
pub const LDELEM_U2: Opcode = Opcode::new(
    "ldelem.u2",
    PopRefPopI,
    PushI,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0x93,
    Next,
);
pub const LDELEM_I4: Opcode = Opcode::new(
    "ldelem.i4",
    PopRefPopI,
    PushI,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0x94,
    Next,
);
pub const LDELEM_U4: Opcode = Opcode::new(
    "ldelem.u4",
    PopRefPopI,
    PushI,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0x95,
    Next,
);
pub const LDELEM_I8: Opcode = Opcode::new(
    "ldelem.i8",
    PopRefPopI,
    PushI8,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0x96,
    Next,
);
pub const LDELEM_I: Opcode = Opcode::new(
    "ldelem.i", PopRefPopI, PushI, InlineNone, ObjModel, 1, 0xFF, 0x97, Next,
);
pub const LDELEM_R4: Opcode = Opcode::new(
    "ldelem.r4",
    PopRefPopI,
    PushR4,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0x98,
    Next,
);
pub const LDELEM_R8: Opcode = Opcode::new(
    "ldelem.r8",
    PopRefPopI,
    PushR8,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0x99,
    Next,
);
pub const LDELEM_REF: Opcode = Opcode::new(
    "ldelem.ref",
    PopRefPopI,
    PushRef,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0x9A,
    Next,
);
pub const STELEM_I: Opcode = Opcode::new(
    "stelem.i",
    PopRefPopIPopI,
    Push0,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0x9B,
    Next,
);
pub const STELEM_I1: Opcode = Opcode::new(
    "stelem.i1",
    PopRefPopIPopI,
    Push0,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0x9C,
    Next,
);
pub const STELEM_I2: Opcode = Opcode::new(
    "stelem.i2",
    PopRefPopIPopI,
    Push0,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0x9D,
    Next,
);
pub const STELEM_I4: Opcode = Opcode::new(
    "stelem.i4",
    PopRefPopIPopI,
    Push0,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0x9E,
    Next,
);
pub const STELEM_I8: Opcode = Opcode::new(
    "stelem.i8",
    PopRefPopIPopI8,
    Push0,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0x9F,
    Next,
);
pub const STELEM_R4: Opcode = Opcode::new(
    "stelem.r4",
    PopRefPopIPopR4,
    Push0,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0xA0,
    Next,
);
pub const STELEM_R8: Opcode = Opcode::new(
    "stelem.r8",
    PopRefPopIPopR8,
    Push0,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0xA1,
    Next,
);
pub const STELEM_REF: Opcode = Opcode::new(
    "stelem.ref",
    PopRefPopIPopRef,
    Push0,
    InlineNone,
    ObjModel,
    1,
    0xFF,
    0xA2,
    Next,
);
pub const LDELEM: Opcode = Opcode::new(
    "ldelem", PopRefPopI, Push1, InlineType, ObjModel, 1, 0xFF, 0xA3, Next,
);
pub const STELEM: Opcode = Opcode::new(
    "stelem",
    PopRefPopIPop1,
    Push0,
    InlineType,
    ObjModel,
    1,
    0xFF,
    0xA4,
    Next,
);
pub const UNBOX_ANY: Opcode = Opcode::new(
    "unbox.any",
    PopRef,
    Push1,
    InlineType,
    ObjModel,
    1,
    0xFF,
    0xA5,
    Next,
);
pub const UNUSED5: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xA6, Next,
);
pub const UNUSED6: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xA7, Next,
);
pub const UNUSED7: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xA8, Next,
);
pub const UNUSED8: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xA9, Next,
);
pub const UNUSED9: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xAA, Next,
);
pub const UNUSED10: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xAB, Next,
);
pub const UNUSED11: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xAC, Next,
);
pub const UNUSED12: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xAD, Next,
);
pub const UNUSED13: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xAE, Next,
);
pub const UNUSED14: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xAF, Next,
);
pub const UNUSED15: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xB0, Next,
);
pub const UNUSED16: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xB1, Next,
);
pub const UNUSED17: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xB2, Next,
);
pub const CONV_OVF_I1: Opcode = Opcode::new(
    "conv.ovf.i1",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0xB3,
    Next,
);
pub const CONV_OVF_U1: Opcode = Opcode::new(
    "conv.ovf.u1",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0xB4,
    Next,
);
pub const CONV_OVF_I2: Opcode = Opcode::new(
    "conv.ovf.i2",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0xB5,
    Next,
);
pub const CONV_OVF_U2: Opcode = Opcode::new(
    "conv.ovf.u2",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0xB6,
    Next,
);
pub const CONV_OVF_I4: Opcode = Opcode::new(
    "conv.ovf.i4",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0xB7,
    Next,
);
pub const CONV_OVF_U4: Opcode = Opcode::new(
    "conv.ovf.u4",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0xB8,
    Next,
);
pub const CONV_OVF_I8: Opcode = Opcode::new(
    "conv.ovf.i8",
    Pop1,
    PushI8,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0xB9,
    Next,
);
pub const CONV_OVF_U8: Opcode = Opcode::new(
    "conv.ovf.u8",
    Pop1,
    PushI8,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0xBA,
    Next,
);
pub const UNUSED50: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xBB, Next,
);
pub const UNUSED18: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xBC, Next,
);
pub const UNUSED19: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xBD, Next,
);
pub const UNUSED20: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xBE, Next,
);
pub const UNUSED21: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xBF, Next,
);
pub const UNUSED22: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xC0, Next,
);
pub const UNUSED23: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xC1, Next,
);
pub const REFANYVAL: Opcode = Opcode::new(
    "refanyval",
    Pop1,
    PushI,
    InlineType,
    Primitive,
    1,
    0xFF,
    0xC2,
    Next,
);
pub const CKFINITE: Opcode = Opcode::new(
    "ckfinite", Pop1, PushR8, InlineNone, Primitive, 1, 0xFF, 0xC3, Next,
);
pub const UNUSED24: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xC4, Next,
);
pub const UNUSED25: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xC5, Next,
);
pub const MKREFANY: Opcode = Opcode::new(
    "mkrefany", PopI, Push1, InlineType, Primitive, 1, 0xFF, 0xC6, Next,
);
pub const UNUSED59: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xC7, Next,
);
pub const UNUSED60: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xC8, Next,
);
pub const UNUSED61: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xC9, Next,
);
pub const UNUSED62: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xCA, Next,
);
pub const UNUSED63: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xCB, Next,
);
pub const UNUSED64: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xCC, Next,
);
pub const UNUSED65: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xCD, Next,
);
pub const UNUSED66: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xCE, Next,
);
pub const UNUSED67: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xCF, Next,
);
pub const LDTOKEN: Opcode = Opcode::new(
    "ldtoken", Pop0, PushI, InlineTok, Primitive, 1, 0xFF, 0xD0, Next,
);
pub const CONV_U2: Opcode = Opcode::new(
    "conv.u2", Pop1, PushI, InlineNone, Primitive, 1, 0xFF, 0xD1, Next,
);
pub const CONV_U1: Opcode = Opcode::new(
    "conv.u1", Pop1, PushI, InlineNone, Primitive, 1, 0xFF, 0xD2, Next,
);
pub const CONV_I: Opcode = Opcode::new(
    "conv.i", Pop1, PushI, InlineNone, Primitive, 1, 0xFF, 0xD3, Next,
);
pub const CONV_OVF_I: Opcode = Opcode::new(
    "conv.ovf.i",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0xD4,
    Next,
);
pub const CONV_OVF_U: Opcode = Opcode::new(
    "conv.ovf.u",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0xD5,
    Next,
);
pub const ADD_OVF: Opcode = Opcode::new(
    "add.ovf", Pop1Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0xD6, Next,
);
pub const ADD_OVF_UN: Opcode = Opcode::new(
    "add.ovf.un",
    Pop1Pop1,
    Push1,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0xD7,
    Next,
);
pub const MUL_OVF: Opcode = Opcode::new(
    "mul.ovf", Pop1Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0xD8, Next,
);
pub const MUL_OVF_UN: Opcode = Opcode::new(
    "mul.ovf.un",
    Pop1Pop1,
    Push1,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0xD9,
    Next,
);
pub const SUB_OVF: Opcode = Opcode::new(
    "sub.ovf", Pop1Pop1, Push1, InlineNone, Primitive, 1, 0xFF, 0xDA, Next,
);
pub const SUB_OVF_UN: Opcode = Opcode::new(
    "sub.ovf.un",
    Pop1Pop1,
    Push1,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0xDB,
    Next,
);
pub const ENDFINALLY: Opcode = Opcode::new(
    "endfinally",
    Pop0,
    Push0,
    InlineNone,
    Primitive,
    1,
    0xFF,
    0xDC,
    Return,
);
pub const LEAVE: Opcode = Opcode::new(
    "leave",
    Pop0,
    Push0,
    InlineBrTarget,
    Primitive,
    1,
    0xFF,
    0xDD,
    Branch,
);
pub const LEAVE_S: Opcode = Opcode::new(
    "leave.s",
    Pop0,
    Push0,
    ShortInlineBrTarget,
    Primitive,
    1,
    0xFF,
    0xDE,
    Branch,
);
pub const STIND_I: Opcode = Opcode::new(
    "stind.i", PopIPopI, Push0, InlineNone, Primitive, 1, 0xFF, 0xDF, Next,
);
pub const CONV_U: Opcode = Opcode::new(
    "conv.u", Pop1, PushI, InlineNone, Primitive, 1, 0xFF, 0xE0, Next,
);
pub const UNUSED26: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xE1, Next,
);
pub const UNUSED27: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xE2, Next,
);
pub const UNUSED28: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xE3, Next,
);
pub const UNUSED29: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xE4, Next,
);
pub const UNUSED30: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xE5, Next,
);
pub const UNUSED31: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xE6, Next,
);
pub const UNUSED32: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xE7, Next,
);
pub const UNUSED33: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xE8, Next,
);
pub const UNUSED34: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xE9, Next,
);
pub const UNUSED35: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xEA, Next,
);
pub const UNUSED36: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xEB, Next,
);
pub const UNUSED37: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xEC, Next,
);
pub const UNUSED38: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xED, Next,
);
pub const UNUSED39: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xEE, Next,
);
pub const UNUSED40: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xEF, Next,
);
pub const UNUSED41: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xF0, Next,
);
pub const UNUSED42: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xF1, Next,
);
pub const UNUSED43: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xF2, Next,
);
pub const UNUSED44: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xF3, Next,
);
pub const UNUSED45: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xF4, Next,
);
pub const UNUSED46: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xF5, Next,
);
pub const UNUSED47: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xF6, Next,
);
pub const UNUSED48: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 1, 0xFF, 0xF7, Next,
);
pub const PREFIX7: Opcode = Opcode::new(
    "prefix7", Pop0, Push0, InlineNone, Internal, 1, 0xFF, 0xF8, Meta,
);
pub const PREFIX6: Opcode = Opcode::new(
    "prefix6", Pop0, Push0, InlineNone, Internal, 1, 0xFF, 0xF9, Meta,
);
pub const PREFIX5: Opcode = Opcode::new(
    "prefix5", Pop0, Push0, InlineNone, Internal, 1, 0xFF, 0xFA, Meta,
);
pub const PREFIX4: Opcode = Opcode::new(
    "prefix4", Pop0, Push0, InlineNone, Internal, 1, 0xFF, 0xFB, Meta,
);
pub const PREFIX3: Opcode = Opcode::new(
    "prefix3", Pop0, Push0, InlineNone, Internal, 1, 0xFF, 0xFC, Meta,
);
pub const PREFIX2: Opcode = Opcode::new(
    "prefix2", Pop0, Push0, InlineNone, Internal, 1, 0xFF, 0xFD, Meta,
);
pub const PREFIX1: Opcode = Opcode::new(
    "prefix1", Pop0, Push0, InlineNone, Internal, 1, 0xFF, 0xFE, Meta,
);
pub const PREFIXREF: Opcode = Opcode::new(
    "prefixref",
    Pop0,
    Push0,
    InlineNone,
    Internal,
    1,
    0xFF,
    0xFF,
    Meta,
);
pub const ARGLIST: Opcode = Opcode::new(
    "arglist", Pop0, PushI, InlineNone, Primitive, 2, 0xFE, 0x00, Next,
);
pub const CEQ: Opcode = Opcode::new(
    "ceq", Pop1Pop1, PushI, InlineNone, Primitive, 2, 0xFE, 0x01, Next,
);
pub const CGT: Opcode = Opcode::new(
    "cgt", Pop1Pop1, PushI, InlineNone, Primitive, 2, 0xFE, 0x02, Next,
);
pub const CGT_UN: Opcode = Opcode::new(
    "cgt.un", Pop1Pop1, PushI, InlineNone, Primitive, 2, 0xFE, 0x03, Next,
);
pub const CLT: Opcode = Opcode::new(
    "clt", Pop1Pop1, PushI, InlineNone, Primitive, 2, 0xFE, 0x04, Next,
);
pub const CLT_UN: Opcode = Opcode::new(
    "clt.un", Pop1Pop1, PushI, InlineNone, Primitive, 2, 0xFE, 0x05, Next,
);
pub const LDFTN: Opcode = Opcode::new(
    "ldftn",
    Pop0,
    PushI,
    InlineMethod,
    Primitive,
    2,
    0xFE,
    0x06,
    Next,
);
pub const LDVIRTFTN: Opcode = Opcode::new(
    "ldvirtftn",
    PopRef,
    PushI,
    InlineMethod,
    Primitive,
    2,
    0xFE,
    0x07,
    Next,
);
pub const UNUSED56: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 2, 0xFE, 0x08, Next,
);
pub const LDARG: Opcode = Opcode::new(
    "ldarg", Pop0, Push1, InlineVar, Primitive, 2, 0xFE, 0x09, Next,
);
pub const LDARGA: Opcode = Opcode::new(
    "ldarga", Pop0, PushI, InlineVar, Primitive, 2, 0xFE, 0x0A, Next,
);
pub const STARG: Opcode = Opcode::new(
    "starg", Pop1, Push0, InlineVar, Primitive, 2, 0xFE, 0x0B, Next,
);
pub const LDLOC: Opcode = Opcode::new(
    "ldloc", Pop0, Push1, InlineVar, Primitive, 2, 0xFE, 0x0C, Next,
);
pub const LDLOCA: Opcode = Opcode::new(
    "ldloca", Pop0, PushI, InlineVar, Primitive, 2, 0xFE, 0x0D, Next,
);
pub const STLOC: Opcode = Opcode::new(
    "stloc", Pop1, Push0, InlineVar, Primitive, 2, 0xFE, 0x0E, Next,
);
pub const LOCALLOC: Opcode = Opcode::new(
    "localloc", PopI, PushI, InlineNone, Primitive, 2, 0xFE, 0x0F, Next,
);
pub const UNUSED57: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 2, 0xFE, 0x10, Next,
);
pub const ENDFILTER: Opcode = Opcode::new(
    "endfilter",
    PopI,
    Push0,
    InlineNone,
    Primitive,
    2,
    0xFE,
    0x11,
    Return,
);
pub const UNALIGNED: Opcode = Opcode::new(
    "unaligned.",
    Pop0,
    Push0,
    ShortInlineI,
    Prefix,
    2,
    0xFE,
    0x12,
    Meta,
);
pub const VOLATILE: Opcode = Opcode::new(
    "volatile.",
    Pop0,
    Push0,
    InlineNone,
    Prefix,
    2,
    0xFE,
    0x13,
    Meta,
);
pub const TAILCALL: Opcode = Opcode::new(
    "tail.", Pop0, Push0, InlineNone, Prefix, 2, 0xFE, 0x14, Meta,
);
pub const INITOBJ: Opcode = Opcode::new(
    "initobj", PopI, Push0, InlineType, ObjModel, 2, 0xFE, 0x15, Next,
);
pub const CONSTRAINED: Opcode = Opcode::new(
    "constrained.",
    Pop0,
    Push0,
    InlineType,
    Prefix,
    2,
    0xFE,
    0x16,
    Meta,
);
pub const CPBLK: Opcode = Opcode::new(
    "cpblk",
    PopIPopIPopI,
    Push0,
    InlineNone,
    Primitive,
    2,
    0xFE,
    0x17,
    Next,
);
pub const INITBLK: Opcode = Opcode::new(
    "initblk",
    PopIPopIPopI,
    Push0,
    InlineNone,
    Primitive,
    2,
    0xFE,
    0x18,
    Next,
);
pub const UNUSED69: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 2, 0xFE, 0x19, Next,
);
pub const RETHROW: Opcode = Opcode::new(
    "rethrow", Pop0, Push0, InlineNone, ObjModel, 2, 0xFE, 0x1A, Throw,
);
pub const UNUSED51: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 2, 0xFE, 0x1B, Next,
);
pub const SIZEOF: Opcode = Opcode::new(
    "sizeof", Pop0, PushI, InlineType, Primitive, 2, 0xFE, 0x1C, Next,
);
pub const REFANYTYPE: Opcode = Opcode::new(
    "refanytype",
    Pop1,
    PushI,
    InlineNone,
    Primitive,
    2,
    0xFE,
    0x1D,
    Next,
);
pub const READONLY: Opcode = Opcode::new(
    "readonly.",
    Pop0,
    Push0,
    InlineNone,
    Prefix,
    2,
    0xFE,
    0x1E,
    Meta,
);
pub const UNUSED53: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 2, 0xFE, 0x1F, Next,
);
pub const UNUSED54: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 2, 0xFE, 0x20, Next,
);
pub const UNUSED55: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 2, 0xFE, 0x21, Next,
);
pub const UNUSED70: Opcode = Opcode::new(
    "unused", Pop0, Push0, InlineNone, Primitive, 2, 0xFE, 0x22, Next,
);
