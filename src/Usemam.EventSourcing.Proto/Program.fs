namespace Usemam.EventSourcing.Proto

open System
open API
open Usemam.EventSourcing.Proto.Domain
open Usemam.EventSourcing.Proto.Infrastructure
open Usemam.EventSourcing.Proto.Infrastructure.EventStorage

module Helper =

    let printEvents events =
        events
        |> List.map (fun e ->
            match e with
            | TransactionCredit (m, c, a) -> printfn "Credited $%.2f from %s to %s" m c a.Name
            | TransactionDebit (m, a, c) -> printfn "Debited $%.2f from %s to %s" m a.Name c
            | TransactionTransfer (m, a1, a2) -> printfn "Transferred $%.2f from %s to %s" m a1.Name a2.Name
            | _ -> ()
        ) |> ignore
    
    let printAccounts accounts =
        accounts
        |> Map.iter (fun name acc -> printfn "Account '%s' - $%.2f" name acc.Balance)
    
    let printCategories categories =
        categories
        |> Map.iter (fun name balance -> printfn "Category '%s' has $%.2f" name balance)

module Program =

    [<EntryPoint>]
    let main _ =
        let appConfig : EventSourced.EventSourcedConfig<Event, Command, Query> =
            {
                EventStorageInit = InMemoryStorage.initialize
                EventStoreInit = EventStore.initialize
                QueryHandler = QueryHandler.initialize
                    [] // todo
                CommandHandlerInit = CommandHandler.initialize Behavior.behavior
                EventListenerInit = EventListener.initialize
                EventHandlers = [] // todo
            }
        let eventStorage : EventStorage<Event> = InMemoryStorage.initialize()
        let eventStore = EventStore.initialize eventStorage

        let userId = Guid.NewGuid()
        let account =
            {
                Name = "Cash"
                Balance = 0.0
            }

        (* eventStore.Append userId [Credit (100.0, "Income", account)]
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
        Helper.printCategories categories *)
        
        0 // return an integer exit code
