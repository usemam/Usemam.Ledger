namespace Usemam.Ledger.Persistence.Mongo

open System

open MongoDB.Driver

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Transaction

type TransactionsMongo(collection : IMongoCollection<TransactionType>) =
    let sortByDateDesc = Builders<TransactionType>.Sort.Descending("Date")

    interface ITransactions with
        member this.GetEnumerator() =
            collection.Find(Builders<TransactionType>.Filter.Empty).Sort(sortByDateDesc).ToEnumerable().GetEnumerator()

        member this.GetEnumerator() =
            (this :> seq<TransactionType>).GetEnumerator() :> System.Collections.IEnumerator

        member this.between min max =
            let filter =
                Builders<TransactionType>.Filter.And(
                    Builders<TransactionType>.Filter.Gte("Date", min),
                    Builders<TransactionType>.Filter.Lte("Date", max))
            collection.Find(filter).Sort(sortByDateDesc).ToEnumerable()

        member this.push transaction =
            collection.InsertOne(transaction)
            TransactionsMongo(collection) :> ITransactions

        member this.pop () =
            let mostRecent = collection.Find(Builders<TransactionType>.Filter.Empty).Sort(sortByDateDesc).FirstOrDefault()
            if not (isNull (box mostRecent)) then
                let filter = Builders<TransactionType>.Filter.Eq("Date", mostRecent.Date)
                collection.DeleteOne(filter) |> ignore
            TransactionsMongo(collection) :> ITransactions
