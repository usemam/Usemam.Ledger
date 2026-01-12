namespace Usemam.Ledger.Persistence.Mongo

open System.Collections.Generic

open MongoDB.Bson
open MongoDB.Driver

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Account

type AccountsMongo(collection : IMongoCollection<AccountDocument>) =
    let nameToId = Dictionary<string, ObjectId>()
    let idToDoc = Dictionary<ObjectId, AccountDocument>()

    do
        // Load existing accounts into cache
        let existingDocs = collection.Find(Builders<AccountDocument>.Filter.Empty).ToList()
        for doc in existingDocs do
            nameToId.[doc.Name] <- doc._id
            idToDoc.[doc._id] <- doc

    member this.GetIdByName(name : string) : ObjectId option =
        match nameToId.TryGetValue(name) with
        | true, id -> Some id
        | false, _ -> None

    member this.GetAccountById(id : ObjectId) : AccountType option =
        match idToDoc.TryGetValue(id) with
        | true, doc -> Some (Mapping.fromAccountDocument doc)
        | false, _ -> None

    member this.GetAccountByName(name : string) : AccountType option =
        match nameToId.TryGetValue(name) with
        | true, id ->
            match idToDoc.TryGetValue(id) with
            | true, doc -> Some (Mapping.fromAccountDocument doc)
            | false, _ -> None
        | false, _ -> None

    interface IAccounts with
        member this.GetEnumerator() =
            (collection.Find(Builders<AccountDocument>.Filter.Empty).ToEnumerable()
            |> Seq.map Mapping.fromAccountDocument).GetEnumerator()

        member this.GetEnumerator() =
            (this :> seq<AccountType>).GetEnumerator() :> System.Collections.IEnumerator

        member this.getByName name =
            let filter =
                Builders<AccountDocument>.Filter.Regex(
                    "Name",
                    BsonRegularExpression(sprintf "^%s" name, "i"))
            let doc = collection.Find(filter).FirstOrDefault()
            if isNull (box doc) then None
            else Some (Mapping.fromAccountDocument doc)

        member this.add account =
            let doc = Mapping.toAccountDocument account
            collection.InsertOne(doc)
            nameToId.[account.Name] <- doc._id
            idToDoc.[doc._id] <- doc
            AccountsMongo(collection) :> IAccounts

        member this.replace account =
            match nameToId.TryGetValue(account.Name) with
            | true, id ->
                let doc = Mapping.toAccountDocumentWithId id account
                let filter = Builders<AccountDocument>.Filter.Eq("_id", id)
                collection.ReplaceOne(filter, doc) |> ignore
                idToDoc.[id] <- doc
                AccountsMongo(collection) :> IAccounts
            | false, _ ->
                // Account doesn't exist, treat as add
                (this :> IAccounts).add account

        member this.remove account =
            match nameToId.TryGetValue(account.Name) with
            | true, id ->
                let filter = Builders<AccountDocument>.Filter.Eq("_id", id)
                collection.DeleteOne(filter) |> ignore
                nameToId.Remove(account.Name) |> ignore
                idToDoc.Remove(id) |> ignore
            | false, _ -> ()
            AccountsMongo(collection) :> IAccounts
