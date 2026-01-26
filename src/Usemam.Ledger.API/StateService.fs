module Usemam.Ledger.API.StateService

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result
open Usemam.Ledger.Persistence.Json
open Usemam.Ledger.Persistence.Mongo

open Microsoft.Extensions.Configuration

type StorageConfiguration =
    {
        StorageType: string
        AccountsFilePath: string
        TransactionsFilePath: string
        MongoConnectionString: string
        MongoDatabaseName: string
    }
    interface IJsonConfig with
        member this.AccountsFilePath = this.AccountsFilePath
        member this.TransactionsFilePath = this.TransactionsFilePath
    interface IMongoConfig with
        member this.MongoConnectionString = this.MongoConnectionString
        member this.MongoDatabaseName = this.MongoDatabaseName

type IStateService =
    abstract member GetState: unit -> State
    abstract member AddTransactions: TransactionType list -> Result<unit>
    abstract member GetAccountByName: string -> AccountType option

type StateService(configuration: IConfiguration) =
    let loadStorageConfiguration () =
        {
            StorageType = configuration.["StorageType"] |> Option.ofObj |> Option.defaultValue "json"
            AccountsFilePath = configuration.["AccountsFilePath"]
            TransactionsFilePath = configuration.["TransactionsFilePath"]
            MongoConnectionString = configuration.["MongoConnectionString"] |> Option.ofObj |> Option.defaultValue ""
            MongoDatabaseName = configuration.["MongoDatabaseName"] |> Option.ofObj |> Option.defaultValue "ledger"
        }

    let config = loadStorageConfiguration()

    let mutable state =
        let loadResult =
            match config.StorageType.ToLowerInvariant() with
            | "mongodb" | "mongo" ->
                let context = MongoContext(config)
                context.LoadState()
            | _ ->
                let context = JsonContext(config)
                context.LoadState()
        match loadResult with
        | Success tracker -> tracker.state
        | Failure msg -> failwith (sprintf "Failed to load state: %s" msg)

    let saveState newState =
        match config.StorageType.ToLowerInvariant() with
        | "mongodb" | "mongo" ->
            let context = MongoContext(config)
            context.SaveState(newState)
        | _ ->
            let context = JsonContext(config)
            context.SaveState(newState)

    interface IStateService with
        member _.GetState() = state

        member _.GetAccountByName(name: string) =
            state.accounts.getByName name

        member _.AddTransactions(transactions: TransactionType list) =
            try
                // Add each transaction to the state using pushTransaction
                let newState =
                    transactions
                    |> List.fold (fun (s: State) t -> s.pushTransaction t) state

                // Save and update state
                match saveState newState with
                | Success () ->
                    state <- newState
                    Success ()
                | Failure msg ->
                    Failure msg
            with ex ->
                Failure ex.Message
