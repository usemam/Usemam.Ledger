open System

open Usemam.Ledger.Console.ColorPrint
open Usemam.Ledger.Console.Parser
open Usemam.Ledger.Domain

[<EntryPoint>]
let main argv = 

    let rec innerLoop f =
        if f() then innerLoop f

    let readCommandAndRun () =
        let input = System.Console.In.ReadLine()
        let parseResult = parse input
        match parseResult with
        | Success (command, _) ->
            cprintf ConsoleColor.Green "Success! Command is "
            printfn "%A" command
            command <> Exit
        | Failure message ->
            cprintf ConsoleColor.Red "Error: "
            printfn "%s" message
            true

    innerLoop readCommandAndRun
        
    0 // return an integer exit code
