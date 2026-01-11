namespace Usemam.Ledger.Persistence.Mongo

open MongoDB.Driver

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Account

type AccountsMongo(collection : IMongoCollection<AccountType>) =
    interface IAccounts with
        member this.GetEnumerator() =
            collection.Find(Builders<AccountType>.Filter.Empty).ToEnumerable().GetEnumerator()

        member this.GetEnumerator() =
            (this :> seq<AccountType>).GetEnumerator() :> System.Collections.IEnumerator

        member this.getByName name =
            let filter =
                Builders<AccountType>.Filter.Regex(
                    "Name",
                    MongoDB.Bson.BsonRegularExpression(sprintf "^%s" name, "i"))
            collection.Find(filter).FirstOrDefault()
            |> Option.ofObj

        member this.add account =
            collection.InsertOne(account)
            AccountsMongo(collection) :> IAccounts

        member this.replace account =
            let filter = Builders<AccountType>.Filter.Eq("Name", account.Name)
            collection.ReplaceOne(filter, account) |> ignore
            AccountsMongo(collection) :> IAccounts

        member this.remove account =
            let filter = Builders<AccountType>.Filter.Eq("Name", account.Name)
            collection.DeleteOne(filter) |> ignore
            AccountsMongo(collection) :> IAccounts
