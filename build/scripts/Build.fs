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
open Fake.SystemHelper
open Tooling

module Build =
    
    let private isCI = Environment.hasEnvironVar "BUILD_ID"
    
    let private oldDiagnosticSourceVersion = SemVer.parse "4.6.0"
    
    let mutable private currentDiagnosticSourceVersion = None
    
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
            
            if Directory.Exists bin then
                let buildOutput = Paths.BuildOutput project          
                Shell.copyDir buildOutput bin (fun _ -> true)
        )
        
    let private dotnet target projectOrSln =
        DotNet.Exec [target; projectOrSln; "-c"; "Release"; "-v"; "q"; "--nologo"]
        
    let private msBuild target projectOrSln =
        MSBuild.build (fun p ->
                { p with
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
        
    /// Gets the current version of System.Diagnostics.DiagnosticSource referenced by Elastic.Apm    
    let private getCurrentApmDiagnosticSourceVersion =
        match currentDiagnosticSourceVersion with
        | Some v -> v
        | None ->        
            let manager = AnalyzerManager();
            let analyzer = manager.GetProject(Paths.SrcProjFile "Elastic.Apm")          
            let analyzeResult = analyzer.Build("netstandard2.0").First()        
            let values = analyzeResult.PackageReferences.["System.Diagnostics.DiagnosticSource"]
            let version = SemVer.parse values.["Version"]
            currentDiagnosticSourceVersion <- Some(version)
            version
        
    let private majorVersions = Dictionary<SemVerInfo, SemVerInfo>()

    /// Publishes ElasticApmStartupHook against a 4.x version of System.Diagnostics.DiagnosticSource
    let private publishElasticApmStartupHookWithDiagnosticSourceVersion () =                
        let projects =
            !! (Paths.SrcProjFile "Elastic.Apm")
            ++ (Paths.SrcProjFile "Elastic.Apm.StartupHook.Loader")
        
        projects
        |> Seq.map getAllTargetFrameworks
        |> Seq.iter (fun (proj, frameworks) ->
            frameworks
            |> Seq.iter(fun framework ->
                let output =
                    Path.GetFileNameWithoutExtension proj
                    |> (fun p -> sprintf "%s_%i.0.0/%s" p oldDiagnosticSourceVersion.Major framework)
                    |> Paths.BuildOutput
                    |> Path.GetFullPath
                
                printfn "Publishing %s %s with System.Diagnostics.DiagnosticSource %O..." proj framework oldDiagnosticSourceVersion
                DotNet.Exec ["publish" ; proj
                             sprintf "\"/p:DiagnosticSourceVersion=%O\"" oldDiagnosticSourceVersion
                             "-c"; "Release"
                             "-f"; framework
                             "-v"; "q"
                             "-o"; output
                             "--nologo"; "--force"]
            )
        )

    /// Generates a new .sln file that contains only .NET Core projects  
    let GenerateNetCoreSln () =
        File.Copy(Paths.Solution, Paths.SolutionNetCore, true)
        fullFrameworkProjects
        |> Seq.iter (fun proj -> DotNet.Exec ["sln" ; Paths.SolutionNetCore; "remove"; proj])
        
    /// Runs dotnet build on all .NET core projects in the solution.
    /// When running on Windows and not CI, also runs MSBuild Build on .NET Framework
    let Build () =
        dotnet "build" Paths.SolutionNetCore
        if isWindows && not isCI then msBuild "Build" aspNetFullFramework
        copyBinRelease()
                              
    /// Publishes all projects with framework versions
    let Publish targets =
        
        let projs =
            match targets with
            | Some t -> t
            | None -> allSrcProjects
        
        projs
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
    
    /// Version suffix used for canary builds
    let versionSuffix = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") |> sprintf "alpha-%s"
     
    /// Packages projects into nuget packages
    let Pack (canary:bool) =
        let arguments =
            let a = ["pack" ; Paths.Solution; "-c"; "Release"; "-o"; Paths.NugetOutput]
            if canary then List.append a ["--version-suffix"; versionSuffix]
            else a      
        DotNet.Exec arguments
          
    let Clean () =
        Shell.cleanDir Paths.BuildOutputFolder
        dotnet "clean" Paths.SolutionNetCore       
        if isWindows && not isCI then msBuild "Clean" aspNetFullFramework

    /// Restores all packages for the solution
    let Restore () =
        DotNet.Exec ["restore" ; Paths.SolutionNetCore; "-v"; "q"]
        if isWindows then DotNet.Exec ["restore" ; aspNetFullFramework; "-v"; "q"]
            
    /// Creates versioned ElasticApmAgent.zip file    
    let AgentZip (canary:bool) =        
        let name = "ElasticApmAgent"      
        let versionedName =
            if canary then
                sprintf "%s_%s-%s" name (Versioning.CurrentVersion.AssemblyVersion.ToString()) versionSuffix
            else
                sprintf "%s_%s" name (Versioning.CurrentVersion.AssemblyVersion.ToString())

        let agentDir = Paths.BuildOutput name |> DirectoryInfo

        let internalizeJsonDotNet pathToDlls = 
            let agentDllPath = sprintf "%s%c%s" pathToDlls Path.DirectorySeparatorChar "Elastic.Apm.dll" 
            let newtonsoftJsonDllPath = sprintf "%s%c%s" pathToDlls Path.DirectorySeparatorChar "Newtonsoft.Json.dll"
            let newNewtonsoftJsonDllPath = sprintf "%s%c%s" pathToDlls Path.DirectorySeparatorChar "ApmNewtonsoft.Json.dll"
            let args = ["-i"; agentDllPath; "-o "; agentDllPath; "-i "; newtonsoftJsonDllPath;  "-o"; newNewtonsoftJsonDllPath]
            Tooling.Rewriter args

        agentDir.Create()

        // all files of interest are top level files in the source directory
        let copy (destination: DirectoryInfo) (source: DirectoryInfo) =
            source.GetFiles()
            |> Seq.filter (fun file -> file.Extension = ".dll" || file.Extension = ".pdb")
            |> Seq.iter (fun file -> file.CopyTo(Path.combine destination.FullName file.Name, true) |> ignore)

        // copy startup hook to root of agent directory
        !! (Paths.BuildOutput "ElasticApmAgentStartupHook/netcoreapp2.2")
        |> Seq.filter Path.isDirectory
        |> Seq.map DirectoryInfo
        |> Seq.iter (copy agentDir)
       
        // assemblies compiled against "current" version of System.Diagnostics.DiagnosticSource    
        let subdirForCurrentDiagnosticSourceVersion = 
            (agentDir.CreateSubdirectory(sprintf "%i.0.0" getCurrentApmDiagnosticSourceVersion.Major))

        !! (Paths.BuildOutput "Elastic.Apm.StartupHook.Loader/netcoreapp2.2")
        ++ (Paths.BuildOutput "Elastic.Apm/netstandard2.0")
        |> Seq.filter Path.isDirectory
        |> Seq.map DirectoryInfo
        |> Seq.iter (copy subdirForCurrentDiagnosticSourceVersion)

        internalizeJsonDotNet (subdirForCurrentDiagnosticSourceVersion.ToString())
        subdirForCurrentDiagnosticSourceVersion.GetFiles("Newtonsoft.Json.dll").FirstOrDefault().Delete()
      
        // assemblies compiled against older version of System.Diagnostics.DiagnosticSource 
        let subdirForOlderDiagnosticSourceVersion = 
            (agentDir.CreateSubdirectory(sprintf "%i.0.0" oldDiagnosticSourceVersion.Major))

        !! (Paths.BuildOutput (sprintf "Elastic.Apm.StartupHook.Loader_%i.0.0/netcoreapp2.2" oldDiagnosticSourceVersion.Major))
        ++ (Paths.BuildOutput (sprintf "Elastic.Apm_%i.0.0/netstandard2.0" oldDiagnosticSourceVersion.Major))
        |> Seq.filter Path.isDirectory
        |> Seq.map DirectoryInfo
        |> Seq.iter (copy subdirForOlderDiagnosticSourceVersion)

        internalizeJsonDotNet (subdirForOlderDiagnosticSourceVersion.ToString())
        subdirForOlderDiagnosticSourceVersion.GetFiles("Newtonsoft.Json.dll").FirstOrDefault().Delete()

        // include version in the zip file name    
        ZipFile.CreateFromDirectory(agentDir.FullName, Paths.BuildOutput versionedName + ".zip")
      
    /// Builds docker image including the ElasticApmAgent  
    let AgentDocker (canary:bool) =
        let agentVersion =
            if canary then
                sprintf "%s-%s" (Versioning.CurrentVersion.AssemblyVersion.ToString()) versionSuffix
            else
                Versioning.CurrentVersion.AssemblyVersion.ToString()        
        
        Docker.Exec [ "build"; "--file"; "./build/docker/Dockerfile";
                      "--tag"; sprintf "observability/apm-agent-dotnet:%s" agentVersion; "./build/output/ElasticApmAgent" ]