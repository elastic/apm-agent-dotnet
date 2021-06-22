pub use self::{
    cor_sig::*, helpers::*, instruction::*, method::*, method_header::*, opcode::*, section::*,
};

mod cor_sig;
mod helpers;
mod il_rewriter;
mod instruction;
mod method;
mod method_header;
mod opcode;
mod section;

pub const MAX_LENGTH: u32 = 1024;
