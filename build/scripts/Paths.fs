// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Scripts

module Paths =

    let OwnerName = "elastic"
    let RepositoryName = "apm-agent-dotnet"
    let Repository = sprintf "https://github.com/%s/%s/" OwnerName RepositoryName

    let BuildFolder = "build"
    let TargetsFolder = "build/scripts"
    
    let BuildOutputFolder = sprintf "%s/output" BuildFolder
    
    /// A path to a folder in the build output folder
    let BuildOutput(folder) = sprintf "%s/%s" BuildOutputFolder folder
    
    let Tool tool = sprintf "packages/build/%s" tool
    
    let NugetOutput = sprintf "%s/_packages" BuildOutputFolder
    let SrcFolder = "src"
    let TestFolder = "test"
    let SampleFolder = "sample"
    
    /// All .NET Core and .NET Framework projects
    let Solution = "ElasticApmAgent.sln"
    
    /// All .NET Core projects
    let SolutionNetCore = "ElasticApmAgent.NetCore.sln"
    
    let Keys(keyFile) = sprintf "%s/%s" BuildFolder keyFile
    let Src folder = sprintf "%s/%s" SrcFolder folder
    let Test folder = sprintf "%s/%s" TestFolder folder
    let Sample folder = sprintf "%s/%s" SampleFolder folder
    
    let SrcProjFile project = sprintf "%s/%s/%s.csproj" SrcFolder project project
    let TestProjFile project = sprintf "%s/%s/%s.csproj" TestFolder project project
    let SampleProjFile project = sprintf "%s/%s/%s.csproj" SampleFolder project project
