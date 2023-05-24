module CommandLine

open Argu
open Microsoft.FSharp.Reflection

let runningOnCI = Fake.Core.Environment.hasEnvironVar "CI"
let runningOnWindows = Fake.Core.Environment.isWindows

type Arguments =
    | [<CliPrefix(CliPrefix.None); Hidden; SubCommand>] Restore
    | [<CliPrefix(CliPrefix.None); SubCommand>] Clean
    | [<CliPrefix(CliPrefix.None); SubCommand >] Build
    
    | [<CliPrefix(CliPrefix.None); SubCommand>] StartupHooksZip
    | [<CliPrefix(CliPrefix.None); SubCommand>] StartupHookDocker
    
    | [<CliPrefix(CliPrefix.None); Hidden; SubCommand>] CleanProfiler
    | [<CliPrefix(CliPrefix.None); SubCommand>] ProfilerZip
    
    | [<CliPrefix(CliPrefix.None); SubCommand>] Test
    | [<CliPrefix(CliPrefix.None); SubCommand>] Integrate

    | [<CliPrefix(CliPrefix.None); Hidden; SubCommand>] PristineCheck
    | [<CliPrefix(CliPrefix.None); Hidden; SubCommand>] GeneratePackages
    | [<CliPrefix(CliPrefix.None); Hidden; SubCommand>] ValidatePackages
    | [<CliPrefix(CliPrefix.None); Hidden; SubCommand>] GenerateReleaseNotes
    | [<CliPrefix(CliPrefix.None); Hidden; SubCommand>] GenerateApiChanges
    
    | [<CliPrefix(CliPrefix.None); SubCommand>] Pack

    | [<CliPrefix(CliPrefix.None); Hidden; SubCommand>] CreateReleaseOnGithub
    | [<CliPrefix(CliPrefix.None); SubCommand>] Publish

    | [<Inherit; AltCommandLine("-s")>] SingleTarget of bool
    | [<Inherit>] Token of string
    | [<Inherit; AltCommandLine("-c")>] CleanCheckout of bool

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Restore -> "Restore .NET packages"
            | Clean _ -> "clean known output locations"
            | CleanProfiler -> "Clean the profiler build"
            | Build _ -> "Run build"
            | StartupHooksZip _ -> "package the agent startup hook zip file"
            | ProfilerZip _ -> "package the agent startup hook zip file"
            
            | StartupHookDocker _ -> "Builds the startup hooks docker image"
            
            | Test _ -> "Run all the unit tests"
            | Integrate _ -> "Run all the integration tests "
            | Pack _ -> "runs build, tests, and create and validates the packages shy of publishing them"
            | Publish _ -> "Runs the full release"

            | SingleTarget _ -> "Runs the provided sub command without running their dependencies"
            | Token _ -> "Token to be used to authenticate with github"
            | CleanCheckout _ -> "Skip the clean checkout check that guards the release/publish targets"
            | PristineCheck
            | GeneratePackages
            | ValidatePackages
            | GenerateReleaseNotes
            | GenerateApiChanges
            | CreateReleaseOnGithub -> "Undocumented, dependent target"

    member this.Name =
        match FSharpValue.GetUnionFields(this, typeof<Arguments>) with
        | case, _ -> case.Name.ToLowerInvariant()
        
    member this.Target =
        let case =
            FSharpType.GetUnionCases typeof<Arguments>
            |> Array.find (fun case -> case.Name.ToLowerInvariant() = this.Name.ToLowerInvariant())
        FSharpValue.MakeUnion(case,[||]) :?> Arguments
        
    static member AllTargets =
        FSharpType.GetUnionCases typeof<Arguments>
        |> Array.filter (fun case -> case.GetFields().Length = 0)
        |> Array.map (fun case -> FSharpValue.MakeUnion(case,[||]) :?> Arguments)
                      
        