// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
