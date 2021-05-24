#[derive(Debug)]
#[non_exhaustive]
pub enum Error {
    InvalidMethodHeader,
    InvalidSectionHeader,
    InvalidCil,
    InvalidCilOpcode,
    PreludeTooBig,
    InvalidVersion,
}
