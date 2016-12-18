open System

open Usemam.Ledger.Console.ColorPrint
open Usemam.Ledger.Console.Parser
open Usemam.Ledger.Console.Storage
open Usemam.Ledger.Console.Services

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

[<EntryPoint>]
let main argv = 

    let error message =
        cprintf ConsoleColor.Red "Error: "
        printfn "%s" message

    let appState = loadState()
    
    let rec innerLoop run s =
        match run s with
        | Success (repeat, s') ->
            if repeat then innerLoop run s'
        | Failure message ->
            error message
            innerLoop run s

    let readCommandAndRunService stateResult =
        result {
            let! state = stateResult
            cprintf ConsoleColor.Yellow "> "
            let input = System.Console.In.ReadLine()
            let! command = parse input
            let service = fromCommand command
            let newState = service state
            return command <> Exit, newState
        }

    innerLoop readCommandAndRunService appState
        
    0 // return an integer exit code
