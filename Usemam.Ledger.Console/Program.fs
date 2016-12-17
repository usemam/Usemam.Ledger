﻿open System

open Usemam.Ledger.Console.ColorPrint
open Usemam.Ledger.Console.Parser
open Usemam.Ledger.Console.Services
open Usemam.Ledger.Domain

[<EntryPoint>]
let main argv = 

    let appState = State(Account.AccountsInMemory [], Transaction.TransactionsInMemory [])
    
    let rec innerLoop f s =
        let repeat, s' = f(s)
        if repeat then innerLoop f s'

    let readCommandAndRunService state =
        let error message =
            cprintf ConsoleColor.Red "Error: "
            printfn "%s" message

        let runService service =
            match service state with
            | Success newState -> newState
            | Failure message ->
                error message
                state

        cprintf ConsoleColor.Yellow "> "
        let input = System.Console.In.ReadLine()
        let parseResult = parse input
        match parseResult with
        | Success (command, _) ->
            let service = fromCommand command
            let newState = runService service
            command <> Exit, newState
        | Failure message ->
            error message
            true, state

    innerLoop readCommandAndRunService appState
        
    0 // return an integer exit code
