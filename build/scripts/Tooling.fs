// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Scripts

open System
open ProcNet
open ProcNet.Std

module Tooling = 
    type ExecResult = { ExitCode: int; Output: LineOut seq;}
    
    let private defaultTimeout = TimeSpan.FromMinutes 10.
    
    type NoopWriter () =
        interface IConsoleOutWriter with
            member self.Write (_: Exception) = ()
            member self.Write (_: ConsoleOut) = ()
    
    let private defaultConsoleWriter = Some(ConsoleOutColorWriter() :> IConsoleOutWriter)
    
    let private readInWithTimeout (timeout :TimeSpan) workingDir bin (writer: IConsoleOutWriter option) args = 
        let startArgs = StartArguments(bin, args |> List.toArray)
        startArgs.Timeout <- timeout
        startArgs.ConsoleOutWriter <- Option.defaultValue<IConsoleOutWriter> (NoopWriter())  writer
        if (Option.isSome workingDir) then
            startArgs.WorkingDirectory <- Option.defaultValue "" workingDir
        let result = Proc.Start(startArgs)
        
        if not result.Completed then failwithf "process failed to complete within %O: %s" timeout bin
        if not result.ExitCode.HasValue then failwithf "process yielded no exit code: %s" bin
        { ExitCode = result.ExitCode.Value; Output = seq result.ConsoleOut}

    let private execInWithTimeout (timeout :TimeSpan) workingDir bin args = 
        let startArgs = ExecArguments(bin, args |> List.toArray)
        startArgs.Timeout <- timeout
        if (Option.isSome workingDir) then
            startArgs.WorkingDirectory <- Option.defaultValue "" workingDir
        let result = Proc.Exec(startArgs)
        try
            if result > 0 then
                failwithf "process returned %i: %s" result bin
        with
        | :? ProcExecException as ex -> failwithf "%s" ex.Message

    type BuildTooling(timeout, path) =
        let timeout = match timeout with | Some t -> t | None -> defaultTimeout
        member this.Path = path
        member this.ReadQuietIn workingDirectory arguments =
            readInWithTimeout defaultTimeout (Some workingDirectory) this.Path None arguments
        member this.ReadInWithTimeout workingDirectory arguments timeout =
            readInWithTimeout timeout (Some workingDirectory) this.Path defaultConsoleWriter arguments
        member this.ExecInWithTimeout workingDirectory arguments timeout = execInWithTimeout timeout (Some workingDirectory) this.Path arguments
        member this.ExecWithTimeout arguments timeout = execInWithTimeout timeout None this.Path arguments
        member this.ExecIn workingDirectory arguments = this.ExecInWithTimeout workingDirectory arguments timeout
        member this.Exec arguments = this.ExecWithTimeout arguments timeout

    let DotNet = BuildTooling(None, "dotnet")
    
    let Docker = BuildTooling(None, "docker")
    
    let Cargo = BuildTooling(None, "cargo")
    
    let private restoreDotnetTools = lazy(DotNet.Exec ["tool"; "restore"])
    
    let Diff args =
        restoreDotnetTools.Force()    
        let args = args |> String.concat " "
        DotNet.Exec ["assembly-differ"; args]
