// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Scripts

open System
open System.IO
open System.IO.Compression
open System.Runtime.InteropServices
open System.Xml.Linq
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Tooling

module Build =  
    let private isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    
    let private aspNetFullFramework = Paths.SrcProjFile "Elastic.Apm.AspNetFullFramework"
    
    let private allSrcProjects = !! "src/**/*.csproj"
        
    let private fullFrameworkProjects = [
        aspNetFullFramework
        Paths.TestProjFile "Elastic.Apm.AspNetFullFramework.Tests"
        Paths.SampleProjFile "AspNetFullFrameworkSampleApp"
    ]
        
    /// Gets all the Target Frameworks from a project file
    let private getAllTargetFrameworks (p: string) =
        let doc = XElement.Load p          
        let targetFrameworks =
            let targetFrameworks =
                doc.Descendants(XName.op_Implicit "TargetFrameworks")
                |> Seq.map (fun p -> String.split ';' p.Value)
                |> Seq.concat
        
            doc.Descendants(XName.op_Implicit "TargetFramework")
            |> Seq.map (fun p -> p.Value)
            |> Seq.append targetFrameworks
            |> Seq.distinct
            |> Seq.toArray
        
        (p, targetFrameworks)
    
    /// Copy all the bin release outputs to the build/output directory
    let private copyBinRelease () =     
        allSrcProjects
        |> Seq.iter(fun p ->
            let directory = Path.GetDirectoryName p
            let project = Path.GetFileNameWithoutExtension p          
            let bin = Path.combine directory "bin/Release"
            let buildOutput = Paths.BuildOutput project          
            Shell.copyDir buildOutput bin (fun _ -> true)
        )
        
    let private dotnet target projectOrSln =
        DotNet.Exec [target; projectOrSln; "-c"; "Release"; "-v"; "q"; "--nologo"]
        
    let private msBuild target projectOrSln =     
        MSBuild.build (fun p -> {
           p with
            Verbosity = Some(Quiet)
            Targets = [target]
            Properties = [
                "Configuration", "Release"
                "Optimize", "True"
            ]
            // current version of Fake MSBuild module does not support latest bin log file
            // version of MSBuild in VS 16.8, so disable for now.
            DisableInternalBinLog = true
            NoLogo = true
        }) projectOrSln
    
    /// Generates a new .sln file that contains only .NET Core projects  
    let GenerateNetCoreSln () =
        File.Copy(Paths.Solution, Paths.SolutionNetCore, true)
        fullFrameworkProjects
        |> Seq.iter (fun proj -> DotNet.Exec ["sln" ; Paths.SolutionNetCore; "remove"; proj])
        
    /// Runs dotnet build on all .NET core projects in the solution.
    /// When running on Windows, also runs MSBuild Build on .NET Framework
    let Build () =
        dotnet "build" Paths.SolutionNetCore
        if isWindows then msBuild "Build" aspNetFullFramework
        copyBinRelease()
                
    /// Publishes all projects with framework versions
    let Publish () =
        allSrcProjects
        |> Seq.map getAllTargetFrameworks
        |> Seq.iter (fun (proj, frameworks) ->
            frameworks
            |> Seq.iter(fun framework ->
                printfn "Publishing %s %s..." proj framework
                DotNet.Exec ["publish" ; proj; "-c"; "Release"; "-f"; framework; "-v"; "q"; "--nologo"]
            )
        )
        
        copyBinRelease()
        
    /// Packages projects into nuget packages
    let Pack (canary:bool) =
        let arguments =
            let a = ["pack" ; Paths.Solution; "-c"; "Release"; "-o"; Paths.NugetOutput]
            if canary then List.append a ["--version-suffix"; DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") |> sprintf "alpha-%s"]
            else a      
        DotNet.Exec arguments
          
    let Clean () =
        Shell.cleanDir Paths.BuildOutputFolder
        dotnet "clean" Paths.SolutionNetCore       
        if isWindows then msBuild "Clean" aspNetFullFramework

    /// Restores all packages for the solution
    let Restore () =
        DotNet.Exec ["restore" ; Paths.SolutionNetCore; "-v"; "q"]
        if isWindows then DotNet.Exec ["restore" ; aspNetFullFramework; "-v"; "q"]
            
    /// Creates versioned ElasticApmAgent.zip file    
    let AgentZip () =
        let name = sprintf "ElasticApmAgent_%s" (Versioning.CurrentVersion.AssemblyVersion.ToString())        
        let agentDir = Paths.BuildOutput name |> DirectoryInfo                    
        agentDir.Create()

        let rec copyRecursive (destination: DirectoryInfo) (source: DirectoryInfo) =
            source.GetDirectories()
            |> Seq.iter (fun dir -> copyRecursive (destination.CreateSubdirectory dir.Name) dir)          
            source.GetFiles()
            |> Seq.iter (fun file -> file.CopyTo(Path.combine destination.FullName file.Name, true) |> ignore)
            
        let copyToAgentDir = copyRecursive agentDir

        !! (Paths.BuildOutput "**/netstandard2.0/publish")
        ++ (Paths.BuildOutput "ElasticApmStartupHook/netcoreapp2.2/publish")
        |> Seq.filter Path.isDirectory
        |> Seq.map DirectoryInfo
        |> Seq.iter copyToAgentDir
            
        ZipFile.CreateFromDirectory(agentDir.FullName, Paths.BuildOutput name + ".zip")