// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Scripts

open System.IO
open System.IO.Compression
open System.Xml.Linq
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Tooling

module Build =
    
    let private projectsExceptFullFramework =
        !! "src/**/*.csproj"
        -- "src/**/Elastic.Apm.AspNetFullFramework.csproj"
    
    let private copyBinRelease () =
        // Copy all the bin release outputs
        projectsExceptFullFramework
        |> Seq.iter(fun p ->
            let directory = Path.GetDirectoryName p
            let project = Path.GetFileNameWithoutExtension p
            
            let bin = Path.combine directory "bin/Release"
            let buildOutput = Paths.Output project          
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
            // current version of fake does not support latest bin log file version
            DisableInternalBinLog = true
            NoLogo = true
        }) projectOrSln
    
    let Build () =
        msBuild "Build" Paths.Solution
        copyBinRelease()
        
    /// Publishes all project framework versions
    let Publish () =
        projectsExceptFullFramework
        |> Seq.map (fun p ->
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
            
            (p, targetFrameworks))
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
        Shell.cleanDir Paths.BuildOutput
        msBuild "Clean" Paths.Solution

    let Restore () = DotNet.Exec ["restore"; Paths.Solution]
           
    /// Creates versioned ElasticApmAgent.zip file    
    let AgentZip () =
        let name = sprintf "ElasticApmAgent_%s" (Versioning.CurrentVersion.AssemblyVersion.ToString())        
        let agentDir = Paths.Output name |> DirectoryInfo                    
        agentDir.Create()

        let rec copyRecursive (target: DirectoryInfo) (source: DirectoryInfo)   =
            source.GetDirectories()
            |> Seq.iter (fun dir -> copyRecursive dir (target.CreateSubdirectory dir.Name))
            source.GetFiles()
            |> Seq.iter (fun file -> file.CopyTo(Path.combine target.FullName file.Name, true) |> ignore)
            
        let copyToAgentDir = copyRecursive agentDir

        !! (Paths.Output "/*/netstandard2.0/publish")
        ++ (Paths.Output "ElasticApmStartupHook/netcoreapp2.2/publish")
        |> Seq.map DirectoryInfo
        |> Seq.iter copyToAgentDir
            
        ZipFile.CreateFromDirectory(agentDir.FullName, Paths.Output name + ".zip")