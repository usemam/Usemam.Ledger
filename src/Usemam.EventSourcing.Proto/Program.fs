namespace Usemam.EventSourcing.Proto

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
    
    let printAccounts accounts =
        accounts
        |> Map.iter (fun name acc -> printfn "Account '%s' - $%.2f" name acc.Balance)
    
    let printCategories categories =
        categories
        |> Map.iter (fun name balance -> printfn "Category '%s' has $%.2f" name balance)

module Program =

    [<EntryPoint>]
    let main argv =
        let userId = Guid.NewGuid()
        let eventStore : EventStore<Transaction> = initialize()
        let account =
            {
                Name = "Cash"
                Balance = 0.0
            }
        eventStore.Append userId [Credit (100.0, "Income", account)]
        eventStore.Append userId [Debit (15.0, account, "Grocery")]
        eventStore.Append userId [Debit (7.5, account, "Entertainment")]

        eventStore.GetStream userId
        |> Helper.printEvents
        
        let accounts =
            eventStore.GetStream userId
            |> project accountsProjection
        Helper.printAccounts accounts
        
        let categories =
            eventStore.GetStream userId
            |> project categoriesProjection
        Helper.printCategories categories
        
        0 // return an integer exit code
