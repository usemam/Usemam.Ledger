module Usemam.Ledger.Persistence.Mongo.DataMigration

open System.IO

open MongoDB.Bson
open MongoDB.Driver
open Newtonsoft.Json

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result
open Usemam.Ledger.Persistence.Json

type IMigrationConfig =
    inherit IJsonConfig
    inherit IMongoConfig

let private insertAccounts
    (accountsCollection : IMongoCollection<AccountDocument>)
    (accounts : AccountType list) =
    let accountIdLookup = System.Collections.Generic.Dictionary<string, ObjectId>()
    for account in accounts do
        let doc = Mapping.toAccountDocument account
        accountsCollection.InsertOne(doc)
        accountIdLookup.[account.Name] <- doc._id
    accountIdLookup

let private insertTransactions
    (transactionsCollection : IMongoCollection<TransactionDocument>)
    (getAccountId : string -> ObjectId option)
    (transactions : TransactionType list) =
    let mutable seqNum = 1L
    for transaction in transactions do
        let doc = Mapping.toTransactionDocument seqNum getAccountId transaction
        transactionsCollection.InsertOne(doc)
        seqNum <- seqNum + 1L

let restore (config : IMigrationConfig) =
    result {
        // Load data from JSON files
        let! accountsJson = tryCatch File.ReadAllText config.AccountsFilePath
        let! accounts = tryCatch JsonConvert.DeserializeObject<AccountType list> accountsJson
        let! transactionsJson = tryCatch File.ReadAllText config.TransactionsFilePath
        let! transactions = tryCatch JsonConvert.DeserializeObject<TransactionType list> transactionsJson

        // Connect to MongoDB
        let client = MongoClient(config.MongoConnectionString)
        let database = client.GetDatabase(config.MongoDatabaseName)
        let accountsCollection = database.GetCollection<AccountDocument>("accounts")
        let transactionsCollection = database.GetCollection<TransactionDocument>("transactions")

        // Delete all existing documents (transactions first, then accounts)
        let transactionsFilter = Builders<TransactionDocument>.Filter.Empty
        let accountsFilter = Builders<AccountDocument>.Filter.Empty
        let! _ = tryCatch transactionsCollection.DeleteMany transactionsFilter
        let! _ = tryCatch accountsCollection.DeleteMany accountsFilter

        // Insert accounts and build name -> ObjectId lookup
        let accountIdLookup = insertAccounts accountsCollection accounts

        let getAccountId name =
            if accountIdLookup.ContainsKey(name) then Some accountIdLookup.[name]
            else None

        // Sort transactions by date and insert with sequential sequence numbers
        let sortedTransactions = transactions |> List.sortBy (fun t -> t.Date)
        insertTransactions transactionsCollection getAccountId sortedTransactions

        return ()
    }

let backup (config : IMigrationConfig) =
    result {
        // Connect to MongoDB
        let client = MongoClient(config.MongoConnectionString)
        let database = client.GetDatabase(config.MongoDatabaseName)
        let accountsCollection = database.GetCollection<AccountDocument>("accounts")
        let transactionsCollection = database.GetCollection<TransactionDocument>("transactions")

        // Load all accounts from MongoDB
        let accountDocs = accountsCollection.Find(Builders<AccountDocument>.Filter.Empty).ToList()
        let accounts =
            accountDocs
            |> Seq.map Mapping.fromAccountDocument
            |> Seq.toList

        // Build lookup functions for transaction mapping
        let accountById =
            accountDocs
            |> Seq.map (fun doc -> doc._id, Mapping.fromAccountDocument doc)
            |> dict
        let accountByName =
            accounts
            |> Seq.map (fun a -> a.Name, a)
            |> dict

        let getAccount (id : ObjectId) =
            if accountById.ContainsKey(id) then Some accountById.[id]
            else None
        let getAccountByName name =
            if accountByName.ContainsKey(name) then Some accountByName.[name]
            else None

        // Load all transactions from MongoDB (sorted by SequenceNumber)
        let sort = Builders<TransactionDocument>.Sort.Ascending(fun t -> t.SequenceNumber :> obj)
        let transactionDocs =
            transactionsCollection.Find(Builders<TransactionDocument>.Filter.Empty).Sort(sort).ToList()
        let transactions =
            transactionDocs
            |> Seq.choose (Mapping.fromTransactionDocument getAccount getAccountByName)
            |> Seq.toList

        // Serialize and write to JSON files
        let accountsJson = JsonConvert.SerializeObject(accounts, Formatting.Indented)
        let transactionsJson = JsonConvert.SerializeObject(transactions, Formatting.Indented)

        let! _ = tryCatch (fun () -> File.WriteAllText(config.AccountsFilePath, accountsJson)) ()
        let! _ = tryCatch (fun () -> File.WriteAllText(config.TransactionsFilePath, transactionsJson)) ()

        return ()
    }
