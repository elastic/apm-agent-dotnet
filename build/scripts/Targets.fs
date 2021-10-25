// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Scripts

open System
open System.CommandLine
open System.CommandLine.Invocation
open System.CommandLine.Parsing
open Bullseye
open Fake.IO
open ProcNet
open Fake.Core
open Fake.IO.Globbing.Operators

module Main =  
    let excludeBullsEyeOptions = Set.ofList [
        "--verbose"
        "--clear"
        "--parallel"
    ]
    
    // Command line options for Bullseye, excluding ones where we want to use the name
    // TODO: investigate using sub commands for targets 
    let private bullsEyeOptions =
        Options.Definitions
        |> Seq.filter (fun d -> excludeBullsEyeOptions |> Set.contains d.LongName |> not)
        
    // Command line options for Targets
    let private options : Option list = [
        Option<string>([| "-v"; "--version" |], "The version to use for the build")
        Option<bool>([| "-c"; "--canary" |], "Whether the build is a canary release. Used by pack")
        Option<string>([| "-f"; "--framework" |], "The framework version to use for diffs. Used by diff")
        Option<string[]>([| "-p"; "--packageids" |], "The ids of nuget packages to diff. Used by diff")
    ]
    
    /// Exception relating to passed options/arguments. Used by Bullseye to only include the message if this
    /// type of exception is raised/thrown.
    type OptionException(msg : string) =
        inherit Exception(msg)
        
    [<EntryPoint>] 
    let main args =         
        let cmd = RootCommand()
        cmd.TreatUnmatchedTokensAsErrors <- true
        cmd.Description <- "Runs tasks for the APM .NET agent"      
        options |> Seq.iter cmd.AddOption

        let targets = Argument("targets")
        targets.Arity <- ArgumentArity.ZeroOrMore
        targets.Description <- "The target(s) to run or list"
        cmd.Add(targets)
        
        // Add the Bullseye options as CommandLine options 
        bullsEyeOptions
        |> Seq.iter (fun d ->
            let names =
                [ d.ShortName; d.LongName ]
                |> Seq.filter String.isNotNullOrEmpty
                |> Seq.toArray       
            cmd.Add(Option(names, d.Description))
        )
        
        cmd.Handler <- CommandHandler.Create<ParseResult>(fun (cmdLine: ParseResult) ->
            // parse bullseye targets from commandline targets
            let targets =
                cmdLine.CommandResult.Tokens
                |> Seq.map (fun token -> token.Value)
                
            // Parse the values for Bullseye options from the arguments           
            let parsedBullsEyeOptions =
                let definitions =
                    bullsEyeOptions
                    |> Seq.map (fun o -> struct (o.LongName, cmdLine.ValueForOption<bool>(o.LongName)))
                Options(definitions)

            Targets.Target("version", fun _ -> Versioning.CurrentVersion |> ignore)
            
            Targets.Target("clean", Build.Clean)
            
            Targets.Target("clean-profiler", Build.CleanProfiler)
            
            Targets.Target("netcore-sln", Build.GenerateNetCoreSln)
            
            Targets.Target("restore", ["netcore-sln"], Build.Restore)
           
            Targets.Target("build", ["restore"; "clean"; "version"; "build-profiler"], Build.Build)
            
            Targets.Target("build-profiler", ["restore"; "clean"; "version"; "clean-profiler"], Build.BuildProfiler)
                        
            Targets.Target("profiler-integrations", ["build-profiler"], Build.ProfilerIntegrations)
            
            Targets.Target("profiler-zip", ["profiler-integrations"], fun _ ->
                let projs = !! (Paths.SrcProjFile "Elastic.Apm.Profiler.Managed")
                Build.Publish (Some projs)
                Build.ProfilerZip (cmdLine.ValueForOption<bool>("canary"))
            )
            
            Targets.Target("publish", ["restore"; "clean"; "version"], fun _ -> Build.Publish None)
                  
            Targets.Target("pack", ["agent-zip", "profiler-zip"], fun _ -> Build.Pack (cmdLine.ValueForOption<bool>("canary")))
            
            Targets.Target("agent-zip", ["build"], fun _ ->
                let projs = !! (Paths.SrcProjFile "Elastic.Apm")
                            ++ (Paths.SrcProjFile "Elastic.Apm.StartupHook.Loader")
                
                Build.Publish (Some projs)
                Build.AgentZip (cmdLine.ValueForOption<bool>("canary"))
            )
            
            Targets.Target("agent-docker", ["agent-zip"], fun _ ->
                Build.AgentDocker (cmdLine.ValueForOption<bool>("canary"))
            )   
            
            Targets.Target("release-notes", fun _ ->
                let version = cmdLine.ValueForOption<string>("version")
                let currentVersion = Versioning.CurrentVersion.AssemblyVersion
                
                if String.IsNullOrEmpty version then
                    raise (sprintf "version greater than '%O' is required" currentVersion |> OptionException)
                if SemVer.isValid version = false then
                    raise (OptionException "version must be a valid semantic version")   
                
                let version = SemVer.parse version
                if version <= currentVersion then
                    raise (sprintf "version '%O' must be greater than '%O'" version currentVersion |> OptionException)
              
                ReleaseNotes.GenerateNotes Versioning.CurrentVersion.AssemblyVersion version
            )
            
            Targets.Target("diff", ["build"], fun _ ->
                let version =
                    let v = cmdLine.ValueForOption<string>("version")
                    match v with
                    | null ->
                        // the current version may not have yet been incremented since the last release
                        // so create a patch version from the current version, which will work for both
                        // an incremented and non-incremented cases
                        let c = { Versioning.CurrentVersion.AssemblyVersion with
                                    Patch = Versioning.CurrentVersion.AssemblyVersion.Patch + 1u
                                    Original = None }         
                        c.ToString()
                    | _ -> v
                    
                let framework =
                    let f = cmdLine.ValueForOption<string>("framework")
                    match f with
                    | null -> "netstandard2.0"
                    | _ -> f
                
                let packageDirectories =
                    let p = cmdLine.ValueForOption<string[]>("packageids")
                    match p with
                    | null -> IO.Directory.GetDirectories (Paths.BuildOutputFolder, "Elastic.*")
                    | _ -> p |> Array.map Paths.BuildOutput
                    
                packageDirectories
                |> Array.filter(fun id -> IO.Directory.Exists(Path.combine id framework))
                |> Array.iter(fun dir ->
                    let packageId = IO.Path.GetFileName dir
                    
                    let command = [ sprintf "previous-nuget|%s|%s|%s" packageId version framework
                                    sprintf "directory|%s/%s" dir framework
                                    "-a"; "-f"; "xml"; "--output"; (IO.Path.GetFullPath Paths.BuildOutputFolder)]
                    
                    Tooling.Diff command
                )
            )
            
            // default target if none is specified
            Targets.Target("default", ["build"])

            Targets.RunTargetsAndExit(
                targets,
                parsedBullsEyeOptions,
                (fun e -> e.GetType() = typeof<ProcExecException> || e.GetType() = typeof<OptionException>),
                ":");
        )

        cmd.Invoke(args)