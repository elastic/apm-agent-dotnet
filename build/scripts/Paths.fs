// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Scripts

module Paths =

    let OwnerName = "elastic"
    let RepositoryName = "apm-agent-dotnet"
    let Repository = sprintf "https://github.com/%s/%s/" OwnerName RepositoryName

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
