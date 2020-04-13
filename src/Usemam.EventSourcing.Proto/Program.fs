﻿namespace Usemam.EventSourcing.Proto

open System
open Domain
open EventStore
open Projections

module Helper =

    let printEvents events =
        events
        |> List.map (fun e ->
            match e with
            | Credit (m, c, a) -> printfn "Credited $%.2f from %s to %s" m c a.Name
            | Debit (m, a, c) -> printfn "Debited $%.2f from %s to %s" m a.Name c
            | Transfer (m, a1, a2) -> printfn "Transferred $%.2f from %s to %s" m a1.Name a2.Name
        ) |> ignore
    
    let printAccount name accounts =
        match Map.tryFind name accounts with
        | Some account -> printfn "Account '%s' - $%.2f" account.Name account.Balance
        | None -> printfn "Account '%s' not found" name

module Program =

    [<EntryPoint>]
    let main argv =
        let eventStore : EventStore<Transaction> = initialize()
        let account =
            {
                Name = "Cash"
                Balance = 0.0
            }
        eventStore.Append [Credit (100.0, "Income", account)]
        eventStore.Append [Debit (15.0, account, "Grocery")]
        eventStore.Append [Debit (7.5, account, "Entertainment")]

        eventStore.Get ()
        |> Helper.printEvents
        
        let accounts =
            eventStore.Get ()
            |> project accountsProjection
        
        Helper.printAccount "Cash" accounts
        0 // return an integer exit code
