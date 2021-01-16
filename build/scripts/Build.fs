// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Scripts

open System
open System.Collections.Generic
open System.IO
open System.IO.Compression
open System.Linq
open System.Runtime.InteropServices
open System.Xml.Linq
open Buildalyzer
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
        
    let private elasticApmDiagnosticSourceVersions = Dictionary<bool, string>()
    
    /// Gets the version of System.Diagnostics.DiagnosticSource referenced by Elastic.Apm    
    let private getElasticApmDiagnosticSourceVersion useDiagnosticSourceVersionFour =
        match elasticApmDiagnosticSourceVersions.TryGetValue useDiagnosticSourceVersionFour with
        | true, v -> v
        | false, _ ->        
            let manager = AnalyzerManager();
            let analyzer = manager.GetProject(Paths.SrcProjFile "Elastic.Apm")
            
            if useDiagnosticSourceVersionFour then
                analyzer.SetGlobalProperty("UseDiagnosticSourceVersionFour", "true")
                  
            let analyzeResult = analyzer.Build("netstandard2.0").First()        
            let values = analyzeResult.PackageReferences.["System.Diagnostics.DiagnosticSource"]          
            let version = values.["Version"]
            elasticApmDiagnosticSourceVersions.Add(useDiagnosticSourceVersionFour, version)
            version
        
    let private majorVersions = Dictionary<SemVerInfo, SemVerInfo>()
        
    /// Converts a semantic version to its major version component only 
    let private majorVersion (version: SemVerInfo) =
        match majorVersions.TryGetValue version with
        | true, v -> v
        | false, _ -> 
            let v = { version with
                        Minor = 0u
                        Patch = 0u
                        PreRelease = None
                        Original = None }
            majorVersions.Add(version, v)
            v
        
    /// Publishes ElasticApmStartupHook against a 4.x version of System.Diagnostics.DiagnosticSource
    let private publishElasticApmStartupHookWithDiagnosticSourceVersion () =
        let diagnosticSourceVersion = getElasticApmDiagnosticSourceVersion true         
        let majorVersion = SemVer.parse diagnosticSourceVersion |> majorVersion
           
        let projs =
            !! (Paths.SrcProjFile "Elastic.Apm")
            ++ (Paths.SrcProjFile "Elastic.Apm.StartupHook.Loader")
        
        projs
        |> Seq.map getAllTargetFrameworks
        |> Seq.iter (fun (proj, frameworks) ->
            frameworks
            |> Seq.iter(fun framework ->
                let output =
                    Path.GetFileNameWithoutExtension proj
                    |> (fun p -> sprintf "%s_%O" p majorVersion)
                    |> Paths.BuildOutput
                    |> Path.GetFullPath
                
                printfn "Publishing %s %s with System.Diagnostics.DiagnosticSource %s..." proj framework diagnosticSourceVersion
                DotNet.Exec ["publish" ; proj; "\"/p:UseDiagnosticSourceVersionFour=true\""; "-c"; "Release"; "-f"; framework
                             "-v"; "q"; "--nologo"; "--force"; "-o"; output ]
            )
        )

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
                let output =
                    Path.GetFileNameWithoutExtension proj
                    |> (fun p -> sprintf "%s/%s" p framework) 
                    |> Paths.BuildOutput
                    |> Path.GetFullPath
                
                printfn "Publishing %s %s..." proj framework
                DotNet.Exec ["publish" ; proj; "-c"; "Release"; "-f"; framework; "-v"; "q"; "--nologo"; "-o"; output]
            )
        )
        
        publishElasticApmStartupHookWithDiagnosticSourceVersion()
     
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
            
        let oldDiagnosticSourceVersion = getElasticApmDiagnosticSourceVersion true         
        let oldMajorVersion = SemVer.parse oldDiagnosticSourceVersion |> majorVersion
            
        !! (Paths.BuildOutput "Elastic.Apm.StartupHook.Loader/netcoreapp2.2")
        ++ (Paths.BuildOutput "ElasticApmStartupHook/netcoreapp2.2")       
        |> Seq.filter Path.isDirectory
        |> Seq.map DirectoryInfo
        |> Seq.iter (copyRecursive agentDir)
        
        !! (sprintf "Elastic.Apm.StartupHook.Loader_%O" oldMajorVersion |> Paths.BuildOutput)
        |> Seq.filter Path.isDirectory
        |> Seq.map DirectoryInfo
        |> Seq.iter (copyRecursive (agentDir.CreateSubdirectory(sprintf "%O" oldMajorVersion)))
            
        ZipFile.CreateFromDirectory(agentDir.FullName, Paths.BuildOutput name + ".zip")