namespace Usemam.Ledger.Persistence.Json

open System.IO

open Newtonsoft.Json

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

module private JsonHelpers =
    let deserialize<'T> json = JsonConvert.DeserializeObject<'T> json

type JsonContext(config : IJsonConfig) =

    let saveJson fileName (json : string) = File.WriteAllText(fileName, json)

    let loadJson fileName = File.ReadAllText fileName

    member this.LoadState() =
        result {
            let! accountsJson = tryCatch loadJson config.AccountsFilePath
            let! accounts = tryCatch JsonHelpers.deserialize<AccountType list> accountsJson
            let! transactionsJson = tryCatch loadJson config.TransactionsFilePath
            let! transactions = tryCatch JsonHelpers.deserialize<TransactionType list> transactionsJson
            let state =
                State(
                    AccountsInMemory(accounts) :> Account.IAccounts,
                    TransactionsInMemory(transactions) :> Transaction.ITransactions)
            return CommandTracker(state, [], [])
        }

    member this.SaveState(state : State) =
        let saveCollection collection fileName =
            let json = collection |> Seq.toList |> JsonConvert.SerializeObject
            tryCatch (saveJson fileName) json

        result {
            let! _ = saveCollection state.accounts config.AccountsFilePath
            let! _ = saveCollection state.transactions config.TransactionsFilePath
            return ()
        }
