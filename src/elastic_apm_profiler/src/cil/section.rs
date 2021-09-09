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
    cil::{check_flag, il_u16, il_u32, il_u8},
    error::Error,
};

bitflags! {
    pub struct SectionHeaderFlags: u8 {
        const CorILMethod_Sect_EHTable = 0x1;
        const CorILMethod_Sect_OptILTable = 0x2;
        const CorILMethod_Sect_FatFormat = 0x40;
        const CorILMethod_Sect_MoreSects = 0x80;
    }
}
bitflags! {
    pub struct CorExceptionFlag: u8 {
        const COR_ILEXCEPTION_CLAUSE_NONE = 0x0;
        const COR_ILEXCEPTION_CLAUSE_FILTER = 0x1;
        const COR_ILEXCEPTION_CLAUSE_FINALLY = 0x2;
        const COR_ILEXCEPTION_CLAUSE_FAULT = 0x4;
        const COR_ILEXCEPTION_CLAUSE_DUPLICATED = 0x8;
    }
}
#[derive(Debug)]
pub struct FatSectionHeader {
    pub is_eh_table: bool,
    pub more_sects: bool,
    /// Note that this really should be u24, but no such type exists.
    /// Must take care when converting back to CIL bytes.
    pub data_size: u32,
}

impl FatSectionHeader {
    pub const LENGTH: usize = 4;
    pub fn into_bytes(&self) -> Vec<u8> {
        let mut bytes = Vec::with_capacity(4);
        let mut flags = SectionHeaderFlags::CorILMethod_Sect_FatFormat.bits();
        if self.is_eh_table {
            flags |= SectionHeaderFlags::CorILMethod_Sect_EHTable.bits();
        }
        if self.more_sects {
            flags |= SectionHeaderFlags::CorILMethod_Sect_MoreSects.bits();
        }
        bytes.push(flags);
        bytes.extend_from_slice(&self.data_size.to_le_bytes()[0..3]);
        bytes
    }
}

#[derive(Debug)]
pub struct FatSectionClause {
    pub flag: CorExceptionFlag,
    pub try_offset: u32,
    pub try_length: u32,
    pub handler_offset: u32,
    pub handler_length: u32,
    pub class_token_or_filter_offset: u32,
}
impl FatSectionClause {
    pub(crate) const LENGTH: usize = 24;
    pub fn from_bytes(il: &[u8]) -> Result<Self, Error> {
        let flag = CorExceptionFlag::from_bits(il[0]).unwrap();
        let try_offset = il_u32(il, 4)?;
        let try_length = il_u32(il, 8)?;
        let handler_offset = il_u32(il, 12)?;
        let handler_length = il_u32(il, 16)?;
        let class_token_or_filter_offset = il_u32(il, 20)?;
        Ok(FatSectionClause {
            flag,
            try_offset,
            try_length,
            handler_offset,
            handler_length,
            class_token_or_filter_offset,
        })
    }

    pub fn into_bytes(&self) -> Vec<u8> {
        let mut bytes = Vec::with_capacity(Self::LENGTH);
        let flags = self.flag.bits() as u32;
        bytes.extend_from_slice(&flags.to_le_bytes());
        bytes.extend_from_slice(&self.try_offset.to_le_bytes());
        bytes.extend_from_slice(&self.try_length.to_le_bytes());
        bytes.extend_from_slice(&self.handler_offset.to_le_bytes());
        bytes.extend_from_slice(&self.handler_length.to_le_bytes());
        bytes.extend_from_slice(&self.class_token_or_filter_offset.to_le_bytes());
        bytes
    }
}
#[derive(Debug)]
pub struct SmallSectionHeader {
    pub is_eh_table: bool,
    pub more_sects: bool,
    pub data_size: u8,
}

impl SmallSectionHeader {
    pub fn into_bytes(&self) -> Vec<u8> {
        let mut bytes = Vec::with_capacity(4);
        let mut flags = 0u8;
        if self.is_eh_table {
            flags |= SectionHeaderFlags::CorILMethod_Sect_EHTable.bits();
        }
        if self.more_sects {
            flags |= SectionHeaderFlags::CorILMethod_Sect_MoreSects.bits();
        }
        bytes.push(flags);
        bytes.push(self.data_size);
        bytes.push(0u8); // Padding for DWORD alignment
        bytes.push(0u8); // Padding for DWORD alignment
        bytes
    }
}

#[derive(Debug)]
pub struct SmallSectionClause {
    pub flag: CorExceptionFlag,
    pub try_offset: u16,
    pub try_length: u8,
    pub handler_offset: u16,
    pub handler_length: u8,
    pub class_token_or_filter_offset: u32,
}
impl SmallSectionClause {
    const LENGTH: usize = 12;
    pub fn from_bytes(il: &[u8]) -> Result<Self, Error> {
        let flag = CorExceptionFlag::from_bits(il[0]).unwrap();
        let try_offset = il_u16(il, 2)?;
        let try_length = il_u8(il, 4)?;
        let handler_offset = il_u16(il, 5)?;
        let handler_length = il_u8(il, 7)?;
        let class_token_or_filter_offset = il_u32(il, 8)?;
        Ok(SmallSectionClause {
            flag,
            try_offset,
            try_length,
            handler_offset,
            handler_length,
            class_token_or_filter_offset,
        })
    }

    pub fn into_bytes(&self) -> Vec<u8> {
        let mut bytes = Vec::with_capacity(Self::LENGTH);
        let flags = self.flag.bits() as u16;
        bytes.extend_from_slice(&flags.to_le_bytes());
        bytes.extend_from_slice(&self.try_offset.to_le_bytes());
        bytes.push(self.try_length);
        bytes.extend_from_slice(&self.handler_offset.to_le_bytes());
        bytes.push(self.handler_length);
        bytes.extend_from_slice(&self.class_token_or_filter_offset.to_le_bytes());
        bytes
    }
}
#[derive(Debug)]
pub enum Section {
    FatSection(FatSectionHeader, Vec<FatSectionClause>),
    SmallSection(SmallSectionHeader, Vec<SmallSectionClause>),
}
impl Section {
    pub fn from_bytes(il: &[u8]) -> Result<Self, Error> {
        let header_flags = il[0];
        let is_eh_table = Self::is_eh_table(header_flags);
        let more_sects = Self::more_sects(header_flags);
        if Self::is_small(header_flags) {
            let data_size = il_u8(il, 1)?;
            let small_header = SmallSectionHeader {
                is_eh_table,
                more_sects,
                data_size,
            };
            let clause_bytes = &il[4..(data_size as usize)];
            let clauses = Self::get_small_clauses(clause_bytes)?;
            Ok(Section::SmallSection(small_header, clauses))
        } else if Self::is_fat(header_flags) {
            let byte_1 = il_u8(il, 1)?;
            let byte_2 = il_u8(il, 2)?;
            let byte_3 = il_u8(il, 3)?;
            let data_size = u32::from_le_bytes([byte_1, byte_2, byte_3, 0]);
            let fat_header = FatSectionHeader {
                is_eh_table,
                more_sects,
                data_size,
            };
            let clause_bytes = &il[4..(data_size as usize)];
            let clauses = Self::get_fat_clauses(clause_bytes)?;
            Ok(Section::FatSection(fat_header, clauses))
        } else {
            Err(Error::InvalidSectionHeader)
        }
    }

    pub fn into_bytes(&self) -> Vec<u8> {
        let mut bytes = Vec::new();
        match &self {
            Section::FatSection(header, clauses) => {
                bytes.append(&mut header.into_bytes());
                for clause in clauses.iter() {
                    bytes.append(&mut clause.into_bytes());
                }
            }
            Section::SmallSection(header, clauses) => {
                bytes.append(&mut header.into_bytes());
                for clause in clauses.iter() {
                    bytes.append(&mut clause.into_bytes());
                }
            }
        }
        bytes
    }
    pub fn data_size(&self) -> usize {
        match self {
            Self::FatSection(header, _) => header.data_size as usize,
            Self::SmallSection(header, _) => header.data_size as usize,
        }
    }
    fn is_small(section_header_flags: u8) -> bool {
        !Self::is_fat(section_header_flags)
    }
    fn is_fat(section_header_flags: u8) -> bool {
        check_flag(
            section_header_flags,
            SectionHeaderFlags::CorILMethod_Sect_FatFormat.bits(),
        )
    }
    fn is_eh_table(section_header_flags: u8) -> bool {
        check_flag(
            section_header_flags,
            SectionHeaderFlags::CorILMethod_Sect_EHTable.bits(),
        )
    }
    fn more_sects(section_header_flags: u8) -> bool {
        check_flag(
            section_header_flags,
            SectionHeaderFlags::CorILMethod_Sect_MoreSects.bits(),
        )
    }
    fn get_fat_clauses(il: &[u8]) -> Result<Vec<FatSectionClause>, Error> {
        let mut index = 0;
        let mut clauses = Vec::new();
        while index < il.len() {
            let il = &il[index..];
            let clause = FatSectionClause::from_bytes(il)?;
            index += FatSectionClause::LENGTH;
            clauses.push(clause);
        }
        Ok(clauses)
    }
    fn get_small_clauses(il: &[u8]) -> Result<Vec<SmallSectionClause>, Error> {
        let mut index = 0;
        let mut clauses = Vec::new();
        while index < il.len() {
            let il = &il[index..];
            let clause = SmallSectionClause::from_bytes(il)?;
            index += SmallSectionClause::LENGTH;
            clauses.push(clause);
        }
        Ok(clauses)
    }
}
