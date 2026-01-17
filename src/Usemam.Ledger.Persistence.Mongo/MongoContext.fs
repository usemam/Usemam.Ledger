namespace Usemam.Ledger.Persistence.Mongo

open MongoDB.Driver

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

type MongoContext(config : IMongoConfig) =

    let client = MongoClient(config.MongoConnectionString)
    let database = client.GetDatabase(config.MongoDatabaseName)
    let accountsCollection = database.GetCollection<AccountDocument>("accounts")
    let transactionsCollection = database.GetCollection<TransactionDocument>("transactions")

    member this.LoadState() =
        result {
            let accountsMongo = AccountsMongo(accountsCollection)
            let transactionsMongo = TransactionsMongo(transactionsCollection, accountsMongo)
            let state = State(accountsMongo :> Account.IAccounts, transactionsMongo :> Transaction.ITransactions)
            return CommandTracker(state, [], [])
        }

    member this.SaveState(state : State) =
        // MongoDB implementations persist immediately, so no-op on exit
        Success ()
