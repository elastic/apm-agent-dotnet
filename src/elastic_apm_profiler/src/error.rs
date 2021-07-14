#[derive(Debug)]
#[non_exhaustive]
pub enum Error {
    InvalidMethodHeader,
    InvalidSectionHeader,
    InvalidCil,
    InvalidCilOpcode,
    CodeSize,
    StackSize,
    InvalidVersion,
    InvalidAssemblyReference,
}
