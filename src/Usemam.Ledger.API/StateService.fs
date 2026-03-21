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
    abstract member RunCommand: ICommand -> Result<unit>
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

    let mutable tracker =
        let loadResult =
            match config.StorageType.ToLowerInvariant() with
            | "mongodb" | "mongo" ->
                let context = MongoContext(config)
                context.LoadState()
            | _ ->
                let context = JsonContext(config)
                context.LoadState()
        match loadResult with
        | Success t -> CommandTracker(t.state, [], [])
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
        member _.GetState() = tracker.state

        member _.GetAccountByName(name: string) =
            tracker.state.accounts.getByName name

        member _.RunCommand(cmd: ICommand) =
            try
                match tracker.run cmd with
                | Success newTracker ->
                    match saveState newTracker.state with
                    | Success () ->
                        tracker <- newTracker
                        Success ()
                    | Failure msg -> Failure msg
                | Failure msg -> Failure msg
            with ex ->
                Failure ex.Message
