// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

mod cor;
mod helpers;
mod instruction;
mod method;
mod opcode;
mod section;

pub use self::{
    cor::*, helpers::*, instruction::*, method::*, opcode::*, section::*,
};

pub const MAX_LENGTH: u32 = 1024;
