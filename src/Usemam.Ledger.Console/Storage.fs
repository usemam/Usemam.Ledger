module Usemam.Ledger.Console.Storage

open System.IO

open Newtonsoft.Json

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

open Microsoft.Extensions.Configuration

type private StorageConfiguration =
    {
        AccountsFilePath : string
        TransactionsFilePath : string
    }

let private loadStorageConfiguration () =
    let loadConfig () =
        let configBuilder = ConfigurationBuilder()
        let config = configBuilder.AddJsonFile("appsettings.json", true, true).Build()
        {
            AccountsFilePath = config.Item "AccountsFilePath"
            TransactionsFilePath = config.Item "TransactionsFilePath"
        }
    tryCatch loadConfig ()

let private saveJson fileName (json : string)= File.WriteAllText (fileName, json)

let private loadJson fileName = File.ReadAllText fileName

let saveState (state : State) =
    let saveCollection collection fileName =
        let json = collection |> Seq.toList |> JsonConvert.SerializeObject
        tryCatch (saveJson fileName) json

    result {
        let! config = loadStorageConfiguration()
        let! _ = saveCollection state.accounts config.AccountsFilePath
        let! _ = saveCollection state.transactions config.TransactionsFilePath
        return ()
    }

let private deserialize<'T> json = JsonConvert.DeserializeObject<'T> json

let loadState () =
    result {
        let! config = loadStorageConfiguration()
        let! accountsJson = tryCatch loadJson config.AccountsFilePath
        let! accounts = tryCatch deserialize<AccountType list> accountsJson
        let! transactionsJson = tryCatch loadJson config.TransactionsFilePath
        let! transactions = tryCatch deserialize<TransactionType list> transactionsJson
        let state =
            State(
                Account.AccountsInMemory(accounts),
                Transaction.TransactionsInMemory transactions)
        return CommandTracker(state, [], [])
    }