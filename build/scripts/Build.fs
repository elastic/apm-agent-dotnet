// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Scripts

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
    
    let private aspNetFullFramework = Paths.ProjFile "Elastic.Apm.AspNetFullFramework"
    
    let private projects =
        !! "src/**/*.csproj"
    
    let private projectsExceptFullFramework =
        projects
        -- "src/**/Elastic.Apm.AspNetFullFramework.csproj"
        
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
    
    // Copy all the bin release outputs to the build/output directory
    let private copyBinRelease () =     
        projectsExceptFullFramework
        |> Seq.iter(fun p ->
            let directory = Path.GetDirectoryName p
            let project = Path.GetFileNameWithoutExtension p          
            let bin = Path.combine directory "bin/Release"
            let buildOutput = Paths.BuildOutput project          
            Shell.copyDir buildOutput bin (fun _ -> true)
        )
        
    let private dotnet target projectOrSln =
        DotNet.Exec [target |> String.toLower ; projectOrSln; "-c"; "Release"; "-v"; "q"; "--nologo"]
        
    let private msBuild target projectOrSln =     
        MSBuild.build (fun p -> {
           p with
            Verbosity = Some(Quiet)
            Targets = [target]
            Properties = [
                "Configuration", "Release"
                "Optimize", "True"
            ]
            // current version of Fake does not support latest bin log file version of MSBuild in VS 16.8.
            DisableInternalBinLog = true
            NoLogo = true
        }) projectOrSln
    
    let Build () =
        dotnet "build" Paths.SolutionNetCore
        if isWindows then msBuild "Build" aspNetFullFramework
        copyBinRelease()
                
    /// Publishes all projects with framework versions
    let Publish () =
        projectsExceptFullFramework
        |> Seq.map getAllTargetFrameworks
        |> Seq.iter (fun (proj, frameworks) ->
            let name = Path.GetFileNameWithoutExtension proj
            frameworks
            |> Seq.iter(fun framework ->
                printfn "  Publishing %s %s" name framework
                DotNet.Exec ["publish" ; proj; "-c"; "Release"; "-f"; framework; "-v"; "q"; "--nologo"]
            )
        )
        
        copyBinRelease()
    
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

        let rec copyRecursive (target: DirectoryInfo) (source: DirectoryInfo)   =
            source.GetDirectories()
            |> Seq.iter (fun dir -> copyRecursive dir (target.CreateSubdirectory dir.Name))
            source.GetFiles()
            |> Seq.iter (fun file -> file.CopyTo(Path.combine target.FullName file.Name, true) |> ignore)
            
        let copyToAgentDir = copyRecursive agentDir

        !! (Paths.BuildOutput "/*/netstandard2.0/publish")
        ++ (Paths.BuildOutput "ElasticApmStartupHook/netcoreapp2.2/publish")
        |> Seq.map DirectoryInfo
        |> Seq.iter copyToAgentDir
            
        ZipFile.CreateFromDirectory(agentDir.FullName, Paths.BuildOutput name + ".zip")