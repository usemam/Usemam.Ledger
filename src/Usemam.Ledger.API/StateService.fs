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

    let state =
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

    interface IStateService with
        member _.GetState() = state
