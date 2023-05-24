module Program

open System
open Argu
open Bullseye
open ProcNet
open CommandLine

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>(programName = "./build.sh")
    let args = if Array.isEmpty argv then Array.ofSeq [Build.Name] else argv

    let parsed =
        try
            let parsed = parser.ParseCommandLine(inputs = args, raiseOnUsage = true)
            let arguments = parsed.GetSubCommand()
            Some(parsed, arguments)
        with e ->
            printfn "%s" e.Message
            None

    match parsed with
    | None -> 2
    | Some (parsed, arguments) ->

        let target = arguments.Name

        Targets.Setup parsed arguments
        let swallowTypes = [ typeof<ProcExecException>; typeof<ExceptionExiter> ]

        // temp fix for unit reporting: https://github.com/elastic/apm-pipeline-library/issues/2063
        let exitCode =
            try
                try
                    Targets.RunTargetsWithoutExiting([ target ], (fun e -> swallowTypes |> List.contains (e.GetType())), ":")
                    0
                with
                | :? InvalidUsageException as ex ->
                    Console.WriteLine ex.Message
                    2
                | :? TargetFailedException as ex -> 1
            finally
                Targets.teardown()

        exitCode
