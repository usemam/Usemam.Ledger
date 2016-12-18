module Usemam.Ledger.Console.Storage

open System.IO

open Newtonsoft.Json

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

let saveJson fileName json = File.WriteAllText (fileName, json)

let saveToAccountsFile = saveJson "accounts.db"

let loadJson fileName = File.ReadAllText fileName

let saveState (state : State) =
    let accounts =
        state.accounts |> Seq.toList
    let accountsJson = JsonConvert.SerializeObject accounts
    tryCatch saveToAccountsFile accountsJson

let deserialize<'T> json = JsonConvert.DeserializeObject<'T> json

let loadState () =
    result {
        let! accountsJson = tryCatch loadJson "accounts.db"
        let! accounts = tryCatch deserialize<AccountType list> accountsJson
        return State(Account.AccountsInMemory accounts, Transaction.TransactionsInMemory [])
    }