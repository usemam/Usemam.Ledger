namespace Usemam.Ledger.Persistence.Mongo

open System
open MongoDB.Bson

[<CLIMutable>]
type MoneyDocument =
    { Value : decimal
      Currency : string }

[<CLIMutable>]
type AccountDocument =
    { _id : ObjectId
      Name : string
      IsClosed : bool
      Created : DateTimeOffset
      Balance : MoneyDocument
      Credit : MoneyDocument }

[<CLIMutable>]
type TransactionDescriptionDocument =
    { Type : string
      SourceAccountId : Nullable<ObjectId>
      SourceAccountName : string
      DestAccountId : Nullable<ObjectId>
      DestAccountName : string
      CreditSource : string
      DebitTarget : string }

[<CLIMutable>]
type TransactionDocument =
    { _id : ObjectId
      SequenceNumber : int64
      Date : DateTimeOffset
      Sum : MoneyDocument
      Description : TransactionDescriptionDocument
      TextDescription : string }
