module Usemam.Ledger.Console.Storage

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result
open Usemam.Ledger.Persistence.Json
open Usemam.Ledger.Persistence.Mongo

open Microsoft.Extensions.Configuration

type StorageConfiguration =
    {
        StorageType : string
        AccountsFilePath : string
        TransactionsFilePath : string
        MongoConnectionString : string
        MongoDatabaseName : string
    }
    interface IJsonConfig with
        member this.AccountsFilePath = this.AccountsFilePath
        member this.TransactionsFilePath = this.TransactionsFilePath
    interface IMongoConfig with
        member this.MongoConnectionString = this.MongoConnectionString
        member this.MongoDatabaseName = this.MongoDatabaseName

let private loadStorageConfiguration () =
    let loadConfig () =
        let configBuilder = ConfigurationBuilder()
        let config = configBuilder.AddJsonFile("appsettings.json", true, true).Build()
        {
            StorageType = config.Item "StorageType" |> Option.ofObj |> Option.defaultValue "json"
            AccountsFilePath = config.Item "AccountsFilePath"
            TransactionsFilePath = config.Item "TransactionsFilePath"
            MongoConnectionString = config.Item "MongoConnectionString" |> Option.ofObj |> Option.defaultValue ""
            MongoDatabaseName = config.Item "MongoDatabaseName" |> Option.ofObj |> Option.defaultValue "ledger"
        }
    tryCatch loadConfig ()

let mutable private currentConfig : StorageConfiguration option = None

let loadState () =
    result {
        let! config = loadStorageConfiguration()
        currentConfig <- Some config
        match config.StorageType.ToLowerInvariant() with
        | "mongodb" | "mongo" ->
            let context = MongoContext(config)
            return! context.LoadState()
        | _ ->
            let context = JsonContext(config)
            return! context.LoadState()
    }

let saveState (state : State) =
    match currentConfig with
    | Some config ->
        match config.StorageType.ToLowerInvariant() with
        | "mongodb" | "mongo" ->
            let context = MongoContext(config)
            context.SaveState(state)
        | _ ->
            let context = JsonContext(config)
            context.SaveState(state)
    | None ->
        Failure "Configuration not loaded"
