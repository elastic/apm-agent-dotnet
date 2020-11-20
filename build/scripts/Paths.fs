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
    
    let BuildOutput = sprintf "%s/output" BuildFolder
    let Output(folder) = sprintf "%s/%s" BuildOutput folder
    
    let InplaceBuildOutput project tfm = 
        sprintf "src/%s/bin/Release/%s" project tfm
    let Tool tool = sprintf "packages/build/%s" tool
    let CheckedInToolsFolder = "build/tools"
    let NugetOutput = sprintf "%s/_packages" BuildOutput
    let SourceFolder = "src"   
    let Solution = "ElasticApmAgent.sln"
    
    let Keys(keyFile) = sprintf "%s/%s" BuildFolder keyFile
    let Source(folder) = sprintf "%s/%s" SourceFolder folder
    let TestsSource(folder) = sprintf "tests/%s" folder
    
    let ProjFile project = sprintf "%s/%s/%s.csproj" SourceFolder project project
    let TestProjFile project = sprintf "tests/%s/%s.csproj" project project

    let BinFolder (folder:string) = 
        let f = folder.Replace(@"\", "/")
        sprintf "%s/%s/bin/Release" SourceFolder f
        
        
