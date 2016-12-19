module Usemam.Ledger.Console.Storage

open System.IO

open Newtonsoft.Json

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

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
        return State(Account.AccountsInMemory accounts, Transaction.TransactionsInMemory transactions )
    }