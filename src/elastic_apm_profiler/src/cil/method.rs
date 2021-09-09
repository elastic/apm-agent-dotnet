#![allow(non_upper_case_globals)]

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

use crate::{
    cil::{
        check_flag, il_u32, nearest_multiple, CorExceptionFlag, FatSectionClause, FatSectionHeader,
        Instruction, Opcode,
        Operand::{InlineBrTarget, InlineSwitch, ShortInlineBrTarget},
        Section,
    },
    error::Error,
    ffi::{mdToken, mdTokenNil},
};
use std::{
    convert::TryFrom,
    fmt::{Display, Formatter},
};

bitflags! {
    /// Method header flags
    pub struct CorILMethodFlags: u8 {
        const CorILMethod_FatFormat = 0x3;
        const CorILMethod_TinyFormat = 0x2;
        const CorILMethod_MoreSects = 0x8;
        const CorILMethod_InitLocals = 0x10;
        const CorILMethod_FormatShift = 3;
        const CorILMethod_FormatMask = ((1 << CorILMethodFlags::CorILMethod_FormatShift.bits()) - 1);
        const CorILMethod_SmallFormat = 0x0000;
        const CorILMethod_TinyFormat1 = 0x0006;
    }
}

#[derive(Debug)]
pub struct FatMethodHeader {
    more_sects: bool,
    init_locals: bool,
    max_stack: u16,
    code_size: u32,
    local_var_sig_tok: u32,
}

impl FatMethodHeader {
    pub const SIZE: u8 = 12;
}

#[derive(Debug)]
pub struct TinyMethodHeader {
    code_size: u8,
}

impl TinyMethodHeader {
    pub const MAX_STACK: u8 = 8;
}

#[derive(Debug)]
pub enum MethodHeader {
    Fat(FatMethodHeader),
    Tiny(TinyMethodHeader),
}

impl MethodHeader {
    /// creates a tiny method header
    pub fn tiny(code_size: u8) -> MethodHeader {
        MethodHeader::Tiny(TinyMethodHeader { code_size })
    }

    /// creates a fat method header
    pub fn fat(
        more_sects: bool,
        init_locals: bool,
        max_stack: u16,
        code_size: u32,
        local_var_sig_tok: u32,
    ) -> Self {
        MethodHeader::Fat(FatMethodHeader {
            more_sects,
            init_locals,
            max_stack,
            code_size,
            local_var_sig_tok,
        })
    }

    pub fn from_bytes(method_il: &[u8]) -> Result<Self, Error> {
        let header_flags = method_il[0];
        if Self::is_tiny(header_flags) {
            let code_size = method_il[0] >> (CorILMethodFlags::CorILMethod_FormatShift.bits() - 1);
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
        match &self {
            MethodHeader::Fat(header) => {
                let mut bytes = Vec::with_capacity(FatMethodHeader::SIZE as usize);
                let mut flags = CorILMethodFlags::CorILMethod_FatFormat.bits();
                if header.more_sects {
                    flags |= CorILMethodFlags::CorILMethod_MoreSects.bits();
                }
                if header.init_locals {
                    flags |= CorILMethodFlags::CorILMethod_InitLocals.bits();
                }
                bytes.push(flags);
                bytes.push(FatMethodHeader::SIZE.reverse_bits());
                bytes.extend_from_slice(&header.max_stack.to_le_bytes());
                bytes.extend_from_slice(&header.code_size.to_le_bytes());
                bytes.extend_from_slice(&header.local_var_sig_tok.to_le_bytes());
                bytes
            }
            MethodHeader::Tiny(header) => {
                let byte = header.code_size << 2 | CorILMethodFlags::CorILMethod_TinyFormat.bits();
                vec![byte]
            }
        }
    }

    /// Instructions start and end
    pub fn instructions(&self) -> (usize, usize) {
        match self {
            MethodHeader::Fat(header) => (
                FatMethodHeader::SIZE as usize,
                (FatMethodHeader::SIZE as u32 + header.code_size - 1) as usize,
            ),
            MethodHeader::Tiny(header) => (1, header.code_size as usize),
        }
    }

    pub fn local_var_sig_tok(&self) -> u32 {
        match self {
            MethodHeader::Fat(header) => header.local_var_sig_tok,
            MethodHeader::Tiny(_) => mdTokenNil,
        }
    }

    pub fn set_local_var_sig_tok(&mut self, token: mdToken) {
        match self {
            MethodHeader::Fat(header) => header.local_var_sig_tok = token,
            MethodHeader::Tiny(_) => (),
        }
    }

    pub fn max_stack(&self) -> u16 {
        match self {
            MethodHeader::Fat(header) => header.max_stack,
            MethodHeader::Tiny(_) => TinyMethodHeader::MAX_STACK as u16,
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
            CorILMethodFlags::CorILMethod_MoreSects.bits(),
        )
    }
    fn init_locals(method_header_flags: u8) -> bool {
        check_flag(
            method_header_flags,
            CorILMethodFlags::CorILMethod_InitLocals.bits(),
        )
    }

    fn is_tiny(method_header_flags: u8) -> bool {
        // Check only the 2 least significant bits
        (method_header_flags & 0b00000011) == CorILMethodFlags::CorILMethod_TinyFormat.bits()
    }

    fn is_fat(method_header_flags: u8) -> bool {
        // Check only the 2 least significant bits
        (method_header_flags & 0b00000011) == CorILMethodFlags::CorILMethod_FatFormat.bits()
    }
}

#[derive(Debug)]
pub struct Method {
    /// The starting memory address of the method, if read from IL
    pub address: usize,
    pub header: MethodHeader,
    pub instructions: Vec<Instruction>,
    pub sections: Vec<Section>,
}

impl Method {
    /// Creates a tiny method with the given instructions. If the code size
    /// of the instructions is greater than [u8::MAX], an error result is returned.
    pub fn tiny(instructions: Vec<Instruction>) -> Result<Self, Error> {
        let code_size: usize = instructions.iter().map(|i| i.len()).sum();
        if code_size > u8::MAX as usize {
            Err(Error::InvalidCil)
        } else {
            Ok(Method {
                address: 0,
                header: MethodHeader::tiny(code_size as u8),
                instructions,
                sections: vec![],
            })
        }
    }

    pub fn new(body: &[u8]) -> Result<Self, Error> {
        let address = body.as_ptr() as usize;
        let header = MethodHeader::from_bytes(&body)?;
        let (instructions_start, instructions_end) = header.instructions();
        let instruction_bytes = &body[instructions_start..=instructions_end];
        let instructions = Self::instructions_from_bytes(instruction_bytes)?;
        let sections = match &header {
            MethodHeader::Fat(header) if header.more_sects => {
                // Sections are DWORD aligned
                let sections_start = nearest_multiple(4, instructions_end + 1);
                let sections_bytes = &body[sections_start..];
                Self::sections_from_bytes(sections_bytes)?
            }
            _ => vec![], // only fat headers with the more sections flag set have additional sections
        };
        Ok(Method {
            address,
            header,
            instructions,
            sections,
        })
    }

    /// Expands a tiny method into a fat method
    pub fn expand_tiny_to_fat(&mut self) {
        if let MethodHeader::Tiny(_) = self.header {
            self.header = MethodHeader::Fat(FatMethodHeader {
                more_sects: false,
                init_locals: false,
                max_stack: self.header.max_stack(),
                code_size: self.instructions.iter().map(|i| i.len()).sum::<usize>() as u32,
                local_var_sig_tok: mdTokenNil,
            });
        }
    }

    /// Expands small sections to fat sections
    pub fn expand_small_sections_to_fat(&mut self) {
        match &mut self.header {
            MethodHeader::Fat(header) if header.more_sects => {
                for section in &mut self.sections {
                    if let Section::SmallSection(section_header, clauses) = section {
                        let fat_section_header = FatSectionHeader {
                            is_eh_table: section_header.is_eh_table,
                            more_sects: section_header.more_sects,
                            data_size: (FatSectionHeader::LENGTH
                                + (FatSectionClause::LENGTH * clauses.len()))
                                as u32,
                        };

                        let fat_section_clauses: Vec<FatSectionClause> = clauses
                            .iter()
                            .map(|c| FatSectionClause {
                                flag: c.flag,
                                try_offset: c.try_offset as u32,
                                try_length: c.try_length as u32,
                                handler_offset: c.handler_offset as u32,
                                handler_length: c.handler_length as u32,
                                class_token_or_filter_offset: c.class_token_or_filter_offset,
                            })
                            .collect();
                        *section = Section::FatSection(fat_section_header, fat_section_clauses);
                    }
                }
            }
            _ => (),
        }
    }

    pub fn push_clauses(&mut self, mut clauses: Vec<FatSectionClause>) -> Result<(), Error> {
        match &mut self.header {
            MethodHeader::Fat(method_header) => {
                if method_header.more_sects {
                    // find the last fat section.
                    // Current versions of the CLR allow only one kind of additional section,
                    // an exception handling section
                    let mut idx = -1;
                    for (i, s) in self.sections.iter().enumerate() {
                        if let Section::FatSection(_, _) = s {
                            idx = i as i32;
                        }
                    }

                    if idx == -1 {
                        return Err(Error::InvalidSectionHeader);
                    }
                    let section = self.sections.get_mut(idx as usize).unwrap();
                    if let Section::FatSection(section_header, section_clauses) = section {
                        section_header.is_eh_table = true;
                        section_header.data_size +=
                            (FatSectionClause::LENGTH * clauses.len()) as u32;
                        section_clauses.append(clauses.as_mut());
                    }
                } else {
                    method_header.more_sects = true;
                    self.sections.push(Section::FatSection(
                        FatSectionHeader {
                            is_eh_table: true,
                            more_sects: false,
                            data_size: (FatSectionHeader::LENGTH
                                + (FatSectionClause::LENGTH * clauses.len()))
                                as u32,
                        },
                        clauses,
                    ))
                }

                Ok(())
            }
            _ => Err(Error::InvalidMethodHeader),
        }
    }

    pub fn push_clause(&mut self, clause: FatSectionClause) -> Result<(), Error> {
        self.push_clauses(vec![clause])
    }

    pub fn into_bytes(&self) -> Vec<u8> {
        let mut bytes = Vec::new();
        let mut header_bytes = self.header.into_bytes();
        bytes.append(&mut header_bytes);
        let mut instructions = self.instructions_to_bytes();
        let instructions_len = instructions.len();
        bytes.append(&mut instructions);
        let mut section_bytes = self.sections_to_bytes(instructions_len);
        bytes.append(&mut section_bytes);
        bytes
    }

    /// replaces an instruction at the specified index
    pub fn replace(&mut self, index: usize, instruction: Instruction) -> Result<(), Error> {
        let offset = self.get_offset(index);

        let (existing_len, existing_stack_size) = {
            let existing_instruction = &self.instructions[index];
            (
                existing_instruction.len() as i64,
                existing_instruction.stack_size() as i64,
            )
        };

        let len = instruction.len() as i64;
        let stack_size = instruction.stack_size() as i64;
        let len_diff = len - existing_len;
        let stack_size_diff = stack_size - existing_stack_size;

        self.update_header(len_diff, Some(stack_size_diff))?;
        self.update_sections(offset, len_diff)?;
        self.update_instructions(index, offset, len_diff);
        let _ = self.instructions.remove(index);
        self.instructions.insert(index, instruction);
        Ok(())
    }

    /// inserts an instruction at the specified index
    pub fn insert(&mut self, index: usize, instruction: Instruction) -> Result<(), Error> {
        let len = instruction.len() as i64;
        let stack_size = instruction.stack_size() as i64;
        self.update_header(len, Some(stack_size))?;

        let offset = self.get_offset(index);
        self.update_sections(offset, len)?;
        self.update_instructions(index, offset, len);
        self.instructions.insert(index, instruction);
        Ok(())
    }

    #[inline]
    fn get_offset(&self, index: usize) -> usize {
        self.instructions
            .iter()
            .take(index + 1)
            .map(|i| i.len())
            .sum()
    }

    pub fn get_instruction_offsets(&self) -> Vec<u32> {
        let mut offset = 0;
        let mut offsets = Vec::with_capacity(self.instructions.len());
        for instruction in &self.instructions {
            offsets.push(offset);
            offset += instruction.len() as u32;
        }

        offsets
    }

    fn update_instructions(&mut self, index: usize, offset: usize, len: i64) {
        // update the offsets of control flow instructions and expand any short instructions:
        //
        // 1. for control flow instructions before the target index,
        //    if the offset is positive and results in an index after the target index,
        //    add len to the offset
        // 2. for control flow instructions after the target index,
        //    if the offset is negative and results in an index before the target index,
        //    subtract len from the offset i.e. offset is further away
        let mut map: Vec<usize> = self.instructions.iter().map(|i| i.len()).collect();
        let mut updated_instructions = vec![];
        for (i, instruction) in self.instructions.iter_mut().enumerate() {
            if i < index {
                if let ShortInlineBrTarget(target_offset) = &mut instruction.operand {
                    if *target_offset >= 0 {
                        let mut sum_len = 0;
                        let mut j = 1;
                        while sum_len < *target_offset as usize {
                            sum_len += map[i + j];
                            j += 1;
                        }
                        if i + j > index {
                            let n = *target_offset as i32 + len as i32;
                            if n > i8::MAX as i32 {
                                let current_len = instruction.len();

                                // update the instruction
                                instruction.operand = InlineBrTarget(n);
                                instruction.opcode = Opcode::short_to_long_form(instruction.opcode);

                                // update the map with the new instruction len and record
                                // the original offset and len diff.
                                let new_len = instruction.len();
                                map[i] = new_len;
                                updated_instructions.push((offset, (new_len - current_len) as i64));
                            } else {
                                *target_offset = n as i8;
                            }
                        }
                    }
                } else if let InlineBrTarget(target_offset) = &mut instruction.operand {
                    if *target_offset >= 0 {
                        let mut sum_len = 0;
                        let mut j = 1;
                        while sum_len < *target_offset as usize {
                            sum_len += map[i + j];
                            j += 1;
                        }
                        if i + j > index {
                            let n = *target_offset + len as i32;
                            *target_offset = n;
                        }
                    }
                } else if let InlineSwitch(count, target_offsets) = &mut instruction.operand {
                    for target_offset in target_offsets {
                        if *target_offset >= 0 {
                            let mut sum_len = 0;
                            let mut j = 1;
                            while sum_len < *target_offset as usize {
                                sum_len += map[i + j];
                                j += 1;
                            }
                            if i + j > index {
                                *target_offset += len as i32;
                            }
                        }
                    }
                }
            } else {
                if let ShortInlineBrTarget(target_offset) = &mut instruction.operand {
                    if *target_offset < 0 {
                        let mut sum_len = 0;
                        let mut j = 0;
                        while *target_offset < sum_len {
                            sum_len -= map[i - j] as i8;
                            j += 1;
                        }
                        if i - j < index {
                            let n = *target_offset as i32 - len as i32;
                            if n < i8::MIN as i32 {
                                let current_len = instruction.len();

                                // update the instruction
                                instruction.operand = InlineBrTarget(n);
                                instruction.opcode = Opcode::short_to_long_form(instruction.opcode);

                                // update the map with the new instruction len and record
                                // the original offset and len diff.
                                let new_len = instruction.len();
                                map[i] = new_len;
                                updated_instructions.push((offset, (new_len - current_len) as i64));
                            } else {
                                *target_offset = n as i8;
                            }
                        }
                    }
                } else if let InlineBrTarget(target_offset) = &mut instruction.operand {
                    if *target_offset < 0 {
                        let mut sum_len = 0;
                        let mut j = 0;
                        while *target_offset < sum_len {
                            sum_len -= map[i - j] as i32;
                            j += 1;
                        }
                        if i - j < index {
                            let n = *target_offset - len as i32;
                            *target_offset = n;
                        }
                    }
                } else if let InlineSwitch(count, target_offsets) = &mut instruction.operand {
                    for target_offset in target_offsets {
                        if *target_offset < 0 {
                            let mut sum_len = 0;
                            let mut j = 0;
                            while *target_offset < sum_len {
                                sum_len -= map[i - j] as i32;
                                j += 1;
                            }
                            if i - j < index {
                                *target_offset -= len as i32;
                            }
                        }
                    }
                }
            }
        }

        if !updated_instructions.is_empty() {
            for (offset, len) in updated_instructions {
                self.update_header(len, None).unwrap();
                self.update_sections(offset, len).unwrap();
            }
        }
    }

    fn update_header(&mut self, len: i64, stack_size: Option<i64>) -> Result<(), Error> {
        // update code_size and max_stack in method_header
        match &mut self.header {
            MethodHeader::Fat(header) => {
                let size = header.code_size as i64 + len as i64;
                header.code_size = u32::try_from(size).or(Err(Error::CodeSize))?;
                if let Some(stack_size) = stack_size {
                    let max_stack = header.max_stack as i64 + stack_size as i64;
                    header.max_stack = u16::try_from(max_stack).or(Err(Error::StackSize))?;
                }
            }
            MethodHeader::Tiny(header) => {
                let size = header.code_size as i64 + len as i64;
                header.code_size = u8::try_from(size)
                    .or_else(|err| todo!("Expand tiny into fat header!, {:?}", err))?;
            }
        }

        Ok(())
    }

    fn update_sections(&mut self, offset: usize, len: i64) -> Result<(), Error> {
        // update try offset, try length, handler offset, handler length, and filter offset
        for section in &mut self.sections {
            match section {
                Section::FatSection(_, clauses) => {
                    for clause in clauses {
                        if (offset as u32) <= clause.try_offset {
                            let try_offset = clause.try_offset as i64 + len as i64;
                            clause.try_offset =
                                u32::try_from(try_offset).or(Err(Error::CodeSize))?;
                        } else if (offset as u32) <= clause.try_offset + clause.try_length {
                            let try_length = clause.try_length as i64 + len as i64;
                            clause.try_length =
                                u32::try_from(try_length).or(Err(Error::CodeSize))?;
                        }

                        if (offset as u32) <= clause.handler_offset {
                            let handler_offset = clause.handler_offset as i64 + len as i64;
                            clause.handler_offset =
                                u32::try_from(handler_offset).or(Err(Error::CodeSize))?;
                        } else if (offset as u32) <= clause.handler_offset + clause.handler_length {
                            let handler_length = clause.handler_length as i64 + len as i64;
                            clause.handler_length =
                                u32::try_from(handler_length).or(Err(Error::CodeSize))?;
                        }

                        if clause
                            .flag
                            .contains(CorExceptionFlag::COR_ILEXCEPTION_CLAUSE_FILTER)
                            && (offset as u32) <= clause.class_token_or_filter_offset
                        {
                            let filter_offset =
                                clause.class_token_or_filter_offset as i64 + len as i64;
                            clause.class_token_or_filter_offset =
                                u32::try_from(filter_offset).or(Err(Error::CodeSize))?;
                        }
                    }
                }
                Section::SmallSection(_, clauses) => {
                    for clause in clauses {
                        if (offset as u16) <= clause.try_offset {
                            let try_offset = clause.try_offset as i64 + len as i64;
                            clause.try_offset = u16::try_from(try_offset)
                                .or_else(|err| todo!("Expand into fat section!, {:?}", err))?;
                        } else if (offset as u16) <= clause.try_offset + clause.try_length as u16 {
                            let try_length = clause.try_length as i64 + len as i64;
                            clause.try_length =
                                u8::try_from(try_length).or(Err(Error::CodeSize))?;
                        }

                        if (offset as u16) <= clause.handler_offset {
                            let handler_offset = clause.handler_offset as i64 + len as i64;
                            clause.handler_offset = u16::try_from(handler_offset)
                                .or_else(|err| todo!("Expand into fat section!, {:?}", err))?;
                        } else if (offset as u16)
                            <= clause.handler_offset + clause.handler_length as u16
                        {
                            let handler_length = clause.handler_length as i64 + len as i64;
                            clause.handler_length =
                                u8::try_from(handler_length).or(Err(Error::CodeSize))?;
                        }

                        if clause
                            .flag
                            .contains(CorExceptionFlag::COR_ILEXCEPTION_CLAUSE_FILTER)
                            && (offset as u16) <= clause.class_token_or_filter_offset as u16
                        {
                            let filter_offset =
                                clause.class_token_or_filter_offset as i64 + len as i64;
                            clause.class_token_or_filter_offset =
                                u32::try_from(filter_offset).or(Err(Error::CodeSize))?;
                        }
                    }
                }
            }
        }

        Ok(())
    }

    /// Inserts instructions at the start
    pub fn insert_prelude(&mut self, instructions: Vec<Instruction>) -> Result<(), Error> {
        let len: usize = instructions.iter().map(|i| i.len()).sum();
        let stack_size: usize = instructions.iter().map(|i| i.stack_size()).sum();
        self.update_header(len as i64, Some(stack_size as i64))?;
        self.update_sections(0, len as i64)?;
        self.instructions.splice(0..0, instructions);
        Ok(())
    }

    fn instructions_from_bytes(il: &[u8]) -> Result<Vec<Instruction>, Error> {
        let mut index = 0;
        let mut instructions = Vec::new();
        while index < il.len() {
            let il = &il[index..];
            let instruction = Instruction::from_bytes(il)?;
            index += instruction.len();
            instructions.push(instruction);
        }
        Ok(instructions)
    }

    fn sections_from_bytes(il: &[u8]) -> Result<Vec<Section>, Error> {
        let mut index = 0;
        let mut sections = Vec::new();
        while index < il.len() {
            let il = &il[index..];
            let section = Section::from_bytes(il)?;
            index += section.data_size();
            sections.push(section);
        }
        Ok(sections)
    }

    fn instructions_to_bytes(&self) -> Vec<u8> {
        self.instructions
            .iter()
            .flat_map(|i| i.into_bytes())
            .collect()
    }

    fn sections_to_bytes(&self, instruction_len: usize) -> Vec<u8> {
        let mut bytes = Vec::new();
        match &self.header {
            MethodHeader::Fat(header) if header.more_sects => {
                // Sections must be DWORD aligned. Add zero padding at the end of instructions to achieve alignment.
                let padding_byte_size = nearest_multiple(4, instruction_len) - instruction_len;
                bytes.resize(padding_byte_size, 0);
                let mut section_bytes = self.sections.iter().flat_map(|s| s.into_bytes()).collect();
                bytes.append(&mut section_bytes);
            }
            _ => (),
        }
        bytes
    }
}

impl Display for Method {
    fn fmt(&self, f: &mut Formatter<'_>) -> std::fmt::Result {
        let mut d = f.debug_struct("Method");
        let instructions = self.instructions_to_bytes();
        d.field("header", &self.header.into_bytes())
            .field("instructions", &instructions);

        match &self.header {
            MethodHeader::Fat(header) if header.more_sects => {
                let padding_byte_size =
                    nearest_multiple(4, instructions.len()) - instructions.len();
                if padding_byte_size > 0 {
                    d.field("alignment", &vec![0; padding_byte_size]);
                }
            }
            _ => (),
        };

        if !self.sections.is_empty() {
            d.field(
                "sections",
                &self
                    .sections
                    .iter()
                    .map(|s| s.into_bytes())
                    .collect::<Vec<_>>(),
            );
        }

        d.finish()
    }
}
