// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Scripts

open System
open System.CommandLine
open System.CommandLine.Invocation
open System.CommandLine.Parsing
open Bullseye
open ProcNet
open Fake.Core

module Main =  
    let excludeBullsEyeOptions = Set.ofList [
        "--verbose"
        "--clear"
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
            
            Targets.Target("netcore-sln", Build.GenerateNetCoreSln)
            
            Targets.Target("restore", ["netcore-sln"], Build.Restore)
           
            Targets.Target("build", ["restore"; "clean"; "version"], Build.Build)
            
            Targets.Target("publish", ["restore"; "clean"; "version"], Build.Publish)
            
            Targets.Target("pack", ["build"], fun _ -> Build.Pack (cmdLine.ValueForOption<bool>("canary")))
            
            Targets.Target("agent-zip", ["publish"], Build.AgentZip)           
            
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
            
            // default target if none is specified
            Targets.Target("default", ["build"])

            Targets.RunTargetsAndExit(
                targets,
                parsedBullsEyeOptions,
                (fun e -> e.GetType() = typeof<ProcExecException> || e.GetType() = typeof<OptionException>),
                ":");
        )

        cmd.Invoke(args)