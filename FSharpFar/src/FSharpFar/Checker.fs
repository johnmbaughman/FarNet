﻿[<RequireQualifiedAccess>]
module FSharpFar.Checker
open System
open System.IO
open FSharp.Compiler.CodeAnalysis

[<NoComparison>]
type CheckFileResult = {
    Checker: FSharpChecker
    Options: FSharpProjectOptions
    ParseResults: FSharpParseFileResults
    CheckResults: FSharpCheckFileResults
}

let check file text config = async {
    let checker = FSharpChecker.Create()

    let! options = async {
        // config flags
        let flags = [|
            yield! defaultCompilerArgs
            yield! config.FscArgs
            yield! config.EtcArgs
        |]

        // config files and later others
        let files = ResizeArray()
        let addFiles paths =
            for f in paths do
                let f1 = Path.GetFullPath f
                if not (Seq.containsIgnoreCase f1 files) then
                    files.Add f1
        addFiles config.FscFiles
        addFiles config.EtcFiles

        // .fsx and .fs are different
        if isScriptFileName file then
            // Our flags are used for .fsx #r and #load resolution.
            // SourceFiles: script #load files and the script itself.
            let! options, _errors = checker.GetProjectOptionsFromScript(file, text, otherFlags = flags)

            // add some new files to ours
            addFiles options.SourceFiles

            // result options with combined files
            return { options with SourceFiles = files.ToArray() }
        else
            // add .fs file, it may not be in config
            addFiles [file]

            // make input flags
            let args = [|
                yield! flags
                yield! files
            |]

            // options from just our flags
            return checker.GetProjectOptionsFromCommandLineArgs(file, args)
    }

    let! parseResults, checkAnswer = checker.ParseAndCheckFileInProject(file, 0, text, options)
    let checkResults =
        match checkAnswer with
        | FSharpCheckFileAnswer.Succeeded x -> x
        | FSharpCheckFileAnswer.Aborted -> invalidOp "Unexpected checker abort."

    return {
        Checker = checker
        Options = options
        ParseResults = parseResults
        CheckResults = checkResults
    }
}

let compile config (configPath: string) = async {
    // combine options
    let args = [|
        // required
        yield "fsc.exe"

        // options
        yield! defaultCompilerArgs
        yield! config.FscArgs
        yield! config.OutArgs
        yield "--target:library"

        // sources
        yield! config.FscFiles
        yield! config.OutFiles

        // if output is none make it script
        let hasOutOption = config.OutArgs |> Array.exists (fun x -> x.StartsWith "-o:" || x.StartsWith "--out:")
        if not hasOutOption then
            let name = configPath |> Path.GetFileNameWithoutExtension |> Path.GetFileNameWithoutExtension
            let name = if name.Length > 0 then name else configPath |> Path.GetDirectoryName |> Path.GetFileName
            yield "-o:" + Environment.GetEnvironmentVariable("FARHOME") + $@"\FarNet\Scripts\{name}\{name}.dll"
    |]

    // compile and get errors and exit code
    let checker = FSharpChecker.Create()
    return! checker.Compile args
}
