// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module Paths

open System
open System.IO

    
let Root =
    let mutable dir = DirectoryInfo(".")
    while dir.GetFiles("*.sln").Length = 0 do dir <- dir.Parent
    Environment.CurrentDirectory <- dir.FullName
    dir
    
let RootRelative path = Path.GetRelativePath(Root.FullName, path) 
    
let Output = DirectoryInfo(Path.Combine(Root.FullName, "build", "output"))


let OwnerName = "elastic"
let RepositoryName = "apm-agent-dotnet"
let Repository = sprintf "https://github.com/%s/%s/" OwnerName RepositoryName

let MainTFM = "netstandard2.0"
let Netstandard21TFM = "netstandard2.1"
let SignKey = "002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1"

// TODO inspect below

let BuildFolder = "build"
let SrcFolder = "src"
let TestFolder = "test"
let SampleFolder = "sample"

let BuildOutputFolder = sprintf "%s/output" BuildFolder
let PublishOutputFolder = sprintf "../../%s/output" BuildFolder

/// A path to a folder in the build output folder
let BuildOutput folder = sprintf "%s/%s" BuildOutputFolder folder
/// Used by --property:PublishDir relative to csproject
let PublishDir folder = sprintf "../../%s" <| BuildOutput folder

let NugetOutput = sprintf "%s/_packages" PublishOutputFolder

/// All .NET Core and .NET Framework projects
let Solution = "ElasticApmAgent.sln"

let Keys keyFile = sprintf "%s/%s" BuildFolder keyFile
let Src folder = sprintf "%s/%s" SrcFolder folder
let Test folder = sprintf "%s/%s" TestFolder folder
let Sample folder = sprintf "%s/%s" SampleFolder folder

let SrcProjFile project = sprintf "%s/%s/%s.csproj" SrcFolder project project
let TestProjFile project = sprintf "%s/%s/%s.csproj" TestFolder project project
let SampleProjFile project = sprintf "%s/%s/%s.csproj" SampleFolder project project




