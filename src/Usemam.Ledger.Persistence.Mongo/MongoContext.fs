namespace Usemam.Ledger.Persistence.Mongo

open MongoDB.Driver

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

type MongoContext(config : IMongoConfig) =

    let client = MongoClient(config.MongoConnectionString)
    let database = client.GetDatabase(config.MongoDatabaseName)
    let accountsCollection = database.GetCollection<AccountType>("accounts")
    let transactionsCollection = database.GetCollection<TransactionType>("transactions")

    member this.LoadState() =
        result {
            let accounts = AccountsMongo(accountsCollection) :> Account.IAccounts
            let transactions = TransactionsMongo(transactionsCollection) :> Transaction.ITransactions
            let state = State(accounts, transactions)
            return CommandTracker(state, [], [])
        }

    member this.SaveState(state : State) =
        // MongoDB implementations persist immediately, so no-op on exit
        Success ()
