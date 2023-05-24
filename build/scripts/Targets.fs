// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

module Targets

open Argu
open Fake.Tools.Git
open CommandLine
open System
open Bullseye
open ProcNet
open Scripts
open Fake.IO.Globbing.Operators
open System.IO

let execWithTimeout binary args timeout =
    let opts =
        ExecArguments(binary, args |> List.map (sprintf "\"%s\"") |> List.toArray)
        
    let r = Proc.Exec(opts, timeout)

    match r.HasValue with
    | true -> r.Value
    | false -> failwithf "invocation of `%s` timed out" binary

let exec binary args =
    execWithTimeout binary args (TimeSpan.FromMinutes 4)




type TestMode = | Unit | Integration
let private runTests (arguments: ParseResults<Arguments>) testMode =
    
    let mode = match testMode with | Unit ->  "unit" | Integration -> "integration"
        
    let filterArg =
        match testMode with
        | Unit ->  [ "--filter"; "FullyQualifiedName!~IntegrationTests" ]
        | Integration -> [ "--filter"; "FullyQualifiedName~IntegrationTests" ]

    let os = if runningOnWindows then "win" else "linux"
    let junitOutput =
        Path.Combine(Paths.Output.FullName, $"junit-%s{os}-%s{mode}-{{assembly}}-{{framework}}-test-results.xml")

    let loggerPathArgs = sprintf "LogFilePath=%s" junitOutput
    let loggerArg = $"--logger:\"junit;%s{loggerPathArgs};MethodFormat=Class;FailureBodyFormat=Verbose\""
    let settingsArg = if runningOnCI then (["-s"; ".ci.runsettings"]) else [];

    execWithTimeout "dotnet" ([ "test" ] @ filterArg @ settingsArg @ [ "-c"; "RELEASE"; "-m:1"; loggerArg ]) (TimeSpan.FromMinutes 15)
    |> ignore

let private test (arguments: ParseResults<Arguments>) =
    runTests arguments Unit

let private integrate (arguments: ParseResults<Arguments>) =
    runTests arguments Integration
    
let private pristineCheck (arguments: ParseResults<Arguments>) =
    let doCheck = arguments.TryGetResult CleanCheckout |> Option.defaultValue true

    match doCheck, Information.isCleanWorkingCopy "." with
    | _, true -> printfn "The checkout folder does not have pending changes, proceeding"
    | false, _ -> printf "Checkout is dirty but -c was specified to ignore this"
    | _ -> failwithf "The checkout folder has pending changes, aborting"
    
let private generatePackages (arguments: ParseResults<Arguments>) =
    let output = Paths.RootRelative Paths.Output.FullName
    exec "dotnet" [ "pack"; "-c"; "Release"; "-o"; output ] |> ignore

let private validatePackages (arguments: ParseResults<Arguments>) =
    let output = Paths.RootRelative <| Paths.Output.FullName
    let currentVersion = Versioning.CurrentVersion.FileVersion.ToString()

    let nugetPackages =
        Paths.Output.GetFiles("*.nupkg")
        |> Seq.sortByDescending (fun f -> f.CreationTimeUtc)
        |> Seq.map (fun p -> Paths.RootRelative p.FullName)

    let ciOnWindowsArgs = if runningOnCI && runningOnWindows then [ "-r"; "true" ] else []

    let args =
        [ "-v"; currentVersion; "-k"; Paths.SignKey; "-t"; output ] @ ciOnWindowsArgs

    nugetPackages |> Seq.iter (fun p -> exec "dotnet" ([ "nupkg-validator"; p ] @ args) |> ignore)

let private generateApiChanges (arguments: ParseResults<Arguments>) =
    let output = Paths.RootRelative <| Paths.Output.FullName
    let currentVersion = Versioning.CurrentVersion.FileVersion.ToString()

    let nugetPackages =
        Paths.Output.GetFiles("*.nupkg")
        |> Seq.sortByDescending (fun f -> f.CreationTimeUtc)
        |> Seq.map (fun p ->
            Path
                .GetFileNameWithoutExtension(Paths.RootRelative p.FullName)
                .Replace("." + currentVersion, ""))

    let firstPath project tfms =
        tfms
        |> Seq.map (fun tfm -> (tfm, sprintf "directory|src/%s/bin/Release/%s" project Paths.MainTFM))
        |> Seq.where (fun (tfm, path) -> File.Exists path)
        |> Seq.tryHead

    nugetPackages
    |> Seq.iter (fun p ->
        let outputFile =
            let f = sprintf "breaking-changes-%s.md" p
            Path.Combine(output, f)

        let firstKnownTFM = firstPath p [ Paths.MainTFM; Paths.Netstandard21TFM ]

        match firstKnownTFM with
        | None -> printf "Skipping generating API changes for: %s" p
        | Some (tfm, path) ->
            let args =
                [
                    "assembly-differ"
                    (sprintf "previous-nuget|%s|%s|%s" p currentVersion tfm)
                    (sprintf "directory|%s" path)
                    "-a"
                    "true"
                    "--target"
                    p
                    "-f"
                    "github-comment"
                    "--output"
                    outputFile
                ]

            exec "dotnet" args |> ignore)

let private generateReleaseNotes (arguments: ParseResults<Arguments>) =
    let currentVersion = Versioning.CurrentVersion.FileVersion.ToString()

    let output =
        Paths.RootRelative <| Path.Combine(Paths.Output.FullName, sprintf "release-notes-%s.md" currentVersion)

    let tokenArgs =
        match arguments.TryGetResult Token with
        | None -> []
        | Some token -> [ "--token"; token ]

    let releaseNotesArgs =
        (Paths.Repository.Split("/") |> Seq.toList)
        @ [
            "--version"
            currentVersion
            "--label"
            "enhancement"
            "New Features"
            "--label"
            "bug"
            "Bug Fixes"
            "--label"
            "documentation"
            "Docs Improvements"
          ]
          @ tokenArgs @ [ "--output"; output ]

    exec "dotnet" ([ "release-notes" ] @ releaseNotesArgs) |> ignore

let private createReleaseOnGithub (arguments: ParseResults<Arguments>) =
    let currentVersion = Versioning.CurrentVersion.FileVersion.ToString()

    let tokenArgs =
        match arguments.TryGetResult Token with
        | None -> []
        | Some token -> [ "--token"; token ]

    let releaseNotes =
        Paths.RootRelative <| Path.Combine(Paths.Output.FullName, sprintf "release-notes-%s.md" currentVersion)

    let breakingChanges =
        let breakingChangesDocs = Paths.Output.GetFiles("breaking-changes-*.md")

        breakingChangesDocs |> Seq.map (fun f -> [ "--body"; Paths.RootRelative f.FullName ]) |> Seq.collect id |> Seq.toList

    let releaseArgs =
        (Paths.Repository.Split("/") |> Seq.toList)
        @ [ "create-release"; "--version"; currentVersion; "--body"; releaseNotes ] @ breakingChanges @ tokenArgs

    exec "dotnet" ([ "release-notes" ] @ releaseArgs) |> ignore


// Targets.Target("profiler-zip", ["profiler-integrations"], fun _ ->
let profilerZip (arguments: ParseResults<Arguments>) = 
    
    Build.BuildProfiler()
    printfn "Running profiler-zip..."
    Build.ProfilerIntegrations()
    
    
    let projs = !! (Paths.SrcProjFile "Elastic.Apm.Profiler.Managed")
    Build.Publish(Some projs)
    Build.ProfilerZip()

let startupHooksZip (arguments: ParseResults<Arguments>) = 
    printfn "Running startup hooks zip..."
    let projs = !! (Paths.SrcProjFile "Elastic.Apm")
                ++ (Paths.SrcProjFile "Elastic.Apm.StartupHook.Loader")
    
    Build.Publish(Some projs)
    Build.AgentZip()

// temp fix for unit reporting: https://github.com/elastic/apm-pipeline-library/issues/2063
let teardown () =
    if Paths.Output.Exists then
        let isSkippedFile p =
            File.ReadLines(p) |> Seq.tryHead = Some "<testsuites />"
        Paths.Output.GetFiles("junit-*.xml")
            |> Seq.filter (fun p -> isSkippedFile p.FullName)
            |> Seq.iter (fun f ->
                printfn $"Removing empty test file: %s{f.FullName}"
                f.Delete()
            )
    Console.WriteLine "Ran teardown"

let Setup (parsed: ParseResults<Arguments>) (subCommand: Arguments) =
    let step (name: string) action =
        Targets.Target(name, Action(fun _ -> action (parsed)))

    let cmd (name: string) commandsBefore steps action =
        let singleTarget = (parsed.TryGetResult SingleTarget |> Option.defaultValue false)

        let deps =
            match (singleTarget, commandsBefore) with
            | (true, _) -> []
            | (_, Some d) -> d
            | _ -> []

        let steps = steps |> Option.defaultValue []
        Targets.Target(name, deps @ steps, Action(action))
    
    Arguments.AllTargets
    |> Seq.iter (fun target ->
        match target with
        // steps for commands to depend on
        | CleanProfiler -> step CleanProfiler.Name <| fun _ -> Build.CleanProfiler() 
        | Restore -> step Restore.Name <| fun _ -> Build.Restore() 
        | PristineCheck -> step PristineCheck.Name pristineCheck
        | GeneratePackages -> step GeneratePackages.Name generatePackages
        | ValidatePackages -> step ValidatePackages.Name validatePackages
        | GenerateReleaseNotes -> step GenerateReleaseNotes.Name generateReleaseNotes
        | GenerateApiChanges -> step GenerateApiChanges.Name generateApiChanges
        | CreateReleaseOnGithub -> step CreateReleaseOnGithub.Name createReleaseOnGithub
        
        // sub commands
        | Clean -> cmd Clean.Name None None <| fun _ -> Build.Clean()
        | Build ->
            cmd Build.Name (Some [ Clean.Name ]) (Some [ Restore.Name ])  <| fun _ -> Build.Build()
        | Test ->
            cmd Test.Name (Some [ Build.Name ]) None <| fun _ -> test parsed
        | Integrate ->
            cmd Integrate.Name (Some [ Build.Name ]) None <| fun _ -> () //integrate parsed
            
        | StartupHookDocker ->
            cmd StartupHookDocker.Name None (Some [ StartupHooksZip.Name ])  <| fun _ -> Build.StartupHooksDocker()
        | StartupHooksZip -> 
            cmd StartupHooksZip.Name None (Some [ Build.Name ]) <| fun _ -> startupHooksZip parsed
        | ProfilerZip ->
            cmd ProfilerZip.Name
                (Some [ CleanProfiler.Name ])
                (Some [ Build.Name ])
            <| fun _ -> profilerZip parsed
        
        | Pack -> 
            cmd
                Pack.Name
                (Some [ PristineCheck.Name; Test.Name; Integrate.Name ])
                (Some [
                    GeneratePackages.Name
                    ValidatePackages.Name
                    ProfilerZip.Name
                    StartupHooksZip.Name
                    GenerateReleaseNotes.Name
                    GenerateApiChanges.Name
                ])
            <| fun _ -> printfn "release"
            
        | Publish ->
            cmd Publish.Name (Some [ Pack.Name ]) (Some [ CreateReleaseOnGithub.Name ])
            <| fun _ -> printfn "publish"
            
        // neither steps nor subcommands but command line arguments
        | SingleTarget _ -> ()
        | Token _ -> ()
        | CleanCheckout _ -> ()
    )
    
