namespace Usemam.Ledger.Persistence.Mongo

open System

open MongoDB.Bson
open MongoDB.Driver

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Transaction

type TransactionsMongo(collection : IMongoCollection<TransactionDocument>, accountsMongo : AccountsMongo) =
    let sortBySeqDesc = Builders<TransactionDocument>.Sort.Descending("SequenceNumber")

    let mutable nextSequenceNumber =
        let maxDoc = collection.Find(Builders<TransactionDocument>.Filter.Empty).Sort(sortBySeqDesc).FirstOrDefault()
        if isNull (box maxDoc) then 1L else maxDoc.SequenceNumber + 1L

    let getAccountId = accountsMongo.GetIdByName
    let getAccountById = accountsMongo.GetAccountById
    let getAccountByName = accountsMongo.GetAccountByName

    interface ITransactions with
        member this.GetEnumerator() =
            (collection.Find(Builders<TransactionDocument>.Filter.Empty).Sort(sortBySeqDesc).ToEnumerable()
            |> Seq.choose (Mapping.fromTransactionDocument getAccountById getAccountByName)).GetEnumerator()

        member this.GetEnumerator() =
            (this :> seq<TransactionType>).GetEnumerator() :> System.Collections.IEnumerator

        member this.between min max =
            let filter =
                Builders<TransactionDocument>.Filter.And(
                    Builders<TransactionDocument>.Filter.Gte("Date", min),
                    Builders<TransactionDocument>.Filter.Lte("Date", max))
            collection.Find(filter).Sort(sortBySeqDesc).ToEnumerable()
            |> Seq.choose (Mapping.fromTransactionDocument getAccountById getAccountByName)

        member this.push transaction =
            let doc = Mapping.toTransactionDocument nextSequenceNumber getAccountId transaction
            nextSequenceNumber <- nextSequenceNumber + 1L
            collection.InsertOne(doc)
            TransactionsMongo(collection, accountsMongo) :> ITransactions

        member this.pop () =
            let mostRecent = collection.Find(Builders<TransactionDocument>.Filter.Empty).Sort(sortBySeqDesc).FirstOrDefault()
            if not (isNull (box mostRecent)) then
                let filter = Builders<TransactionDocument>.Filter.Eq("_id", mostRecent._id)
                collection.DeleteOne(filter) |> ignore
            TransactionsMongo(collection, accountsMongo) :> ITransactions
