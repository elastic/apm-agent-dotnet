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
use crate::cli::{
    nearest_multiple, CorExceptionFlag, FatMethodHeader, Instruction, MethodHeader, Opcode,
    Section, TinyMethodHeader, BEQ, BGE, BGT, BRFALSE, BRTRUE,
};
use crate::{
    cli,
    cli::Operand::{InlineBrTarget, InlineSwitch, ShortInlineBrTarget},
    error::Error,
    ffi::{mdSignatureNil, mdTokenNil},
};
use std::{alloc::handle_alloc_error, convert::TryFrom, mem::transmute, slice};

#[derive(Debug)]
pub struct Method {
    /// The starting memory address of the method, if read from IL
    pub address: usize,
    pub header: MethodHeader,
    pub instructions: Vec<Instruction>,
    pub sections: Vec<Section>,
}
impl Method {
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
        let offset = self
            .instructions
            .iter()
            .take(index + 1)
            .map(|i| i.len())
            .sum();

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
        self.update_instructions(index, len_diff);
        let _ = self.instructions.remove(index);
        self.instructions.insert(index, instruction);
        Ok(())
    }

    /// inserts an instruction at the specified index
    pub fn insert(&mut self, index: usize, instruction: Instruction) -> Result<(), Error> {
        let len = instruction.len() as i64;
        let stack_size = instruction.stack_size() as i64;
        self.update_header(len, Some(stack_size))?;

        let offset = self
            .instructions
            .iter()
            .take(index + 1)
            .map(|i| i.len())
            .sum();
        self.update_sections(offset, len)?;
        self.update_instructions(index, len);
        self.instructions.insert(index, instruction);
        Ok(())
    }

    fn get_long_opcode(opcode: Opcode) -> Opcode {
        match opcode {
            cli::BRFALSE_S => cli::BRFALSE,
            cli::BRTRUE_S => cli::BRTRUE,
            cli::BEQ_S => cli::BEQ,
            cli::BGE_S => cli::BGE,
            cli::BGT_S => cli::BGT,
            cli::BLE_S => cli::BLE,
            cli::BLT_S => cli::BLT,
            cli::BR_S => cli::BR,
            cli::BGE_UN_S => cli::BGE_UN,
            cli::BGT_UN_S => cli::BGT_UN,
            cli::BLE_UN_S => cli::BLE_UN,
            cli::BLT_UN_S => cli::BLT_UN,
            cli::BNE_UN_S => cli::BNE_UN,
            _ => opcode,
        }
    }

    fn update_instructions(&mut self, index: usize, len: i64) {
        // update the offsets of control flow instructions:
        //
        // 1. for control flow instructions before the target index,
        //    if the offset is positive and results in an index after the target index,
        //    add len to the offset
        // 2. for control flow instructions after the target index,
        //    if the offset is negative and results in an index before the target index,
        //    subtract len from the offset i.e. offset is further away
        let mut map: Vec<usize> = self.instructions.iter().map(|i| i.len()).collect();
        let current_map = map.clone();
        let mut updated_instructions = vec![];
        for (i, instruction) in self.instructions.iter_mut().enumerate() {
            if i < index {
                if let ShortInlineBrTarget(offset) = instruction.operand {
                    if offset >= 0 {
                        let mut sum_len = 0;
                        let mut j = 1;
                        while sum_len < offset as usize {
                            sum_len += map[i + j];
                            j += 1;
                        }
                        if i + j > index {
                            let n = offset as i32 + len as i32;
                            if n > i8::MAX as i32 {
                                let current_len = instruction.len();
                                let current_offset = current_map.iter().take(i + 1).sum();

                                // update the instruction
                                instruction.operand = InlineBrTarget(n);
                                instruction.opcode = Self::get_long_opcode(instruction.opcode);

                                // update the map with the new instruction len and record
                                // the original offset and len diff.
                                let new_len = instruction.len();
                                map[i] = new_len;
                                updated_instructions
                                    .push((current_offset, (new_len - current_len) as i64));
                            } else {
                                instruction.operand = ShortInlineBrTarget(n as i8);
                            }
                        }
                    }
                } else if let InlineBrTarget(offset) = instruction.operand {
                    if offset >= 0 {
                        let mut sum_len = 0;
                        let mut j = 1;
                        while sum_len < offset as usize {
                            sum_len += map[i + j];
                            j += 1;
                        }
                        if i + j > index {
                            let n = offset + len as i32;
                            instruction.operand = InlineBrTarget(n);
                        }
                    }
                } else if let InlineSwitch(count, offsets) = &instruction.operand {
                    let mut changed = false;
                    let new_offsets = offsets
                        .iter()
                        .map(|&offset| {
                            if offset >= 0 {
                                let mut sum_len = 0;
                                let mut j = 1;
                                while sum_len < offset as usize {
                                    sum_len += map[i + j];
                                    j += 1;
                                }
                                if i + j > index {
                                    changed = true;
                                    return offset + len as i32;
                                }
                            }

                            offset
                        })
                        .collect();
                    if changed {
                        instruction.operand = InlineSwitch(*count, new_offsets);
                    }
                }
            } else {
                if let ShortInlineBrTarget(offset) = instruction.operand {
                    if offset < 0 {
                        let mut sum_len = 0;
                        let mut j = 0;
                        while offset < sum_len {
                            sum_len -= map[i - j] as i8;
                            j += 1;
                        }
                        if i - j < index {
                            let n = offset as i32 - len as i32;
                            if n < i8::MIN as i32 {
                                let current_len = instruction.len();
                                let current_offset = current_map.iter().take(i + 1).sum();

                                // update the instruction
                                instruction.operand = InlineBrTarget(n);
                                instruction.opcode = Self::get_long_opcode(instruction.opcode);

                                // update the map with the new instruction len and record
                                // the original offset and len diff.
                                let new_len = instruction.len();
                                map[i] = new_len;
                                updated_instructions
                                    .push((current_offset, (new_len - current_len) as i64));
                            } else {
                                instruction.operand = ShortInlineBrTarget(n as i8);
                            }
                        }
                    }
                } else if let InlineBrTarget(offset) = instruction.operand {
                    if offset < 0 {
                        let mut sum_len = 0;
                        let mut j = 0;
                        while offset < sum_len {
                            sum_len -= map[i - j] as i32;
                            j += 1;
                        }
                        if i - j < index {
                            let n = offset - len as i32;
                            instruction.operand = InlineBrTarget(n);
                        }
                    }
                } else if let InlineSwitch(count, offsets) = &instruction.operand {
                    let mut changed = false;
                    let new_offsets = offsets
                        .iter()
                        .map(|&offset| {
                            if offset < 0 {
                                let mut sum_len = 0;
                                let mut j = 0;
                                while offset < sum_len {
                                    sum_len -= map[i - j] as i32;
                                    j += 1;
                                }
                                if i - j < index {
                                    changed = true;
                                    return offset - len as i32;
                                }
                            }

                            offset
                        })
                        .collect();
                    if changed {
                        instruction.operand = InlineSwitch(*count, new_offsets);
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
                        if (offset as u32) < clause.try_offset {
                            let try_offset = clause.try_offset as i64 + len as i64;
                            clause.try_offset =
                                u32::try_from(try_offset).or(Err(Error::CodeSize))?;
                        } else if (offset as u32) < clause.try_offset + clause.try_length {
                            let try_length = clause.try_length as i64 + len as i64;
                            clause.try_length =
                                u32::try_from(try_length).or(Err(Error::CodeSize))?;
                        }

                        if (offset as u32) < clause.handler_offset {
                            let handler_offset = clause.handler_offset as i64 + len as i64;
                            clause.handler_offset =
                                u32::try_from(handler_offset).or(Err(Error::CodeSize))?;
                        } else if (offset as u32) < clause.handler_offset + clause.handler_length {
                            let handler_length = clause.handler_length as i64 + len as i64;
                            clause.handler_length =
                                u32::try_from(handler_length).or(Err(Error::CodeSize))?;
                        }

                        if clause
                            .flag
                            .contains(CorExceptionFlag::COR_ILEXCEPTION_CLAUSE_FILTER)
                        {
                            if (offset as u32) < clause.class_token_or_filter_offset {
                                let filter_offset =
                                    clause.class_token_or_filter_offset as i64 + len as i64;
                                clause.class_token_or_filter_offset =
                                    u32::try_from(filter_offset).or(Err(Error::CodeSize))?;
                            }
                        }
                    }
                }
                Section::SmallSection(_, clauses) => {
                    for clause in clauses {
                        if (offset as u16) < clause.try_offset {
                            let try_offset = clause.try_offset as i64 + len as i64;
                            clause.try_offset = u16::try_from(try_offset)
                                .or_else(|err| todo!("Expand into fat section!, {:?}", err))?;
                        } else if (offset as u16) < clause.try_offset + clause.try_length as u16 {
                            let try_length = clause.try_length as i64 + len as i64;
                            clause.try_length =
                                u8::try_from(try_length).or(Err(Error::CodeSize))?;
                        }

                        if (offset as u16) < clause.handler_offset {
                            let handler_offset = clause.handler_offset as i64 + len as i64;
                            clause.handler_offset = u16::try_from(handler_offset)
                                .or_else(|err| todo!("Expand into fat section!, {:?}", err))?;
                        } else if (offset as u16)
                            < clause.handler_offset + clause.handler_length as u16
                        {
                            let handler_length = clause.handler_length as i64 + len as i64;
                            clause.handler_length =
                                u8::try_from(handler_length).or(Err(Error::CodeSize))?;
                        }

                        if clause
                            .flag
                            .contains(CorExceptionFlag::COR_ILEXCEPTION_CLAUSE_FILTER)
                        {
                            if (offset as u16) < clause.class_token_or_filter_offset as u16 {
                                let filter_offset =
                                    clause.class_token_or_filter_offset as i64 + len as i64;
                                clause.class_token_or_filter_offset =
                                    u32::try_from(filter_offset).or(Err(Error::CodeSize))?;
                            }
                        }
                    }
                }
            }
        }

        Ok(())
    }

    /// Inserts instructions at the start
    pub fn insert_prelude(&mut self, instructions: Vec<Instruction>) -> Result<(), Error> {
        // For now assume
        // 1. we aren't adding any new exceptions or new method data sections.
        // 2. we aren't adding any local variables
        // 3. we don't need to expand any short branches, tiny headers, or small sections.
        let len: usize = instructions.iter().map(|i| i.len()).sum();
        let stack_size: usize = instructions.iter().map(|i| i.stack_size()).sum();
        self.update_header(len as i64, Some(stack_size as i64))?;
        self.update_sections(0, len as i64)?;

        // Insert the instructions
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
                for _ in 0..padding_byte_size {
                    bytes.push(0);
                }
                let mut section_bytes = self.sections.iter().flat_map(|s| s.into_bytes()).collect();
                bytes.append(&mut section_bytes);
            }
            _ => (),
        }
        bytes
    }
}
