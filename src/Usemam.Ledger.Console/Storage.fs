module Usemam.Ledger.Console.Storage

open System.IO

open Newtonsoft.Json

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result
open Usemam.Ledger.Backup

open Microsoft.Extensions.Configuration

let saveJson fileName json = File.WriteAllText (fileName, json)

let loadJson fileName = File.ReadAllText fileName

let saveState (state : State) =
    let saveCollection collection fileName =
        let json = collection |> Seq.toList |> JsonConvert.SerializeObject
        tryCatch (saveJson fileName) json

    result {
        let! _ = saveCollection state.accounts "accounts.db"
        let! _ = saveCollection state.transactions "transactions.db"
        return ()
    }

let deserialize<'T> json = JsonConvert.DeserializeObject<'T> json

let loadState () =
    result {
        let! accountsJson = tryCatch loadJson "accounts.db"
        let! accounts = tryCatch deserialize<AccountType list> accountsJson
        let! transactionsJson = tryCatch loadJson "transactions.db"
        let! transactions = tryCatch deserialize<TransactionType list> transactionsJson
        let state =
            State(
                Account.AccountsInMemory(accounts),
                Transaction.TransactionsInMemory transactions)
        return CommandTracker(state, [], [])
    }

let backup () =
    let configBuilder = new ConfigurationBuilder()
    let config = configBuilder.AddJsonFile("appsettings.json", true, true).Build()
    let remoteStorage = new DropboxStorage(config.Item "DropboxAccessToken")
    BackupFacade.run ["accounts.db";"transactions.db"] remoteStorage Clocks.machineClock