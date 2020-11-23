open System

open Usemam.Ledger.Console.ColorPrint
open Usemam.Ledger.Console.Command
open Usemam.Ledger.Console.Parser
open Usemam.Ledger.Console.Storage
open Usemam.Ledger.Console.Services

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

[<EntryPoint>]
let main _ = 

    let error message =
        cprintf ConsoleColor.Red "Error: "
        printfn "%s" message

    let appState = loadState()
    
    let rec readCommandAndRunService stateResult =
        let currentResult =
            result {
                let! state = stateResult
                cprintf ConsoleColor.Yellow "> "
                let input = System.Console.In.ReadLine()
                let! command = parse input
                let service = fromCommand command
                let! newState = service state
                return (not << isExit) command, newState
            }
        match currentResult with
        | Success (repeat, state) ->
            if repeat then readCommandAndRunService (Success state)
        | Failure message ->
            error message
            readCommandAndRunService stateResult

    readCommandAndRunService appState

    match backup() with
    | Success _ -> printfn "%s" "Backup finished"
    | Failure m -> printfn "%s" m
        
    0 // return an integer exit code
