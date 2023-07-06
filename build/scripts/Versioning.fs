namespace Scripts

open Fake.Core

open ProcNet

module Versioning = 
    type AssemblyInfo = {
        AssemblyVersion: SemVerInfo
        InformationalVersion: SemVerInfo
        FileVersion: SemVerInfo
    }
    
    /// Gets the current AssemblyInfo version from the Directory.Build.Props in /src
    let CurrentVersion =
        let version = Proc.Start <| StartArguments("dotnet", "minver",  "-t=v", "-p=canary.0", "-v=e")
        match Seq.toList version.ConsoleOut with
        | [ head ] ->
            match SemVer.isValid(head.Line) with
            | true -> 
                let semver = SemVer.parse(head.Line)
                { AssemblyVersion = semver
                  InformationalVersion = semver
                  FileVersion = semver
                }
            | false -> 
                failwithf "First line from `dotnet-minver '%s' not a valid version`" head.Line;
        | _ ->
            failwithf "failed to run `dotnet-minver` %A " version.ExitCode;
