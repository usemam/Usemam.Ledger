module Usemam.Ledger.Persistence.Mongo.Mapping

open System
open MongoDB.Bson

open Usemam.Ledger.Domain

// Money conversions
let toMoneyDocument (money : Money) : MoneyDocument =
    { Value = money.Amount.Value
      Currency = money.Currency.ToString() }

let fromMoneyDocument (doc : MoneyDocument) : Money =
    let amount = Amount.create doc.Value
    Money(amount, USD)

// Account conversions
let toAccountDocument (account : AccountType) : AccountDocument =
    { _id = ObjectId.GenerateNewId()
      Name = account.Name
      IsClosed = account.IsClosed
      Created = account.Created
      Balance = toMoneyDocument account.Balance
      Credit = toMoneyDocument account.Credit }

let toAccountDocumentWithId (id : ObjectId) (account : AccountType) : AccountDocument =
    { _id = id
      Name = account.Name
      IsClosed = account.IsClosed
      Created = account.Created
      Balance = toMoneyDocument account.Balance
      Credit = toMoneyDocument account.Credit }

let fromAccountDocument (doc : AccountDocument) : AccountType =
    { Name = doc.Name
      IsClosed = doc.IsClosed
      Created = doc.Created
      Balance = fromMoneyDocument doc.Balance
      Credit = fromMoneyDocument doc.Credit }

// Transaction conversions
let toTransactionDocument
    (seqNum : int64)
    (getAccountId : string -> ObjectId option)
    (transaction : TransactionType) : TransactionDocument =

    let descDoc =
        match transaction.Description with
        | Transfer (source, dest) ->
            { Type = "Transfer"
              SourceAccountId = getAccountId source.Name |> Option.toNullable
              SourceAccountName = source.Name
              DestAccountId = getAccountId dest.Name |> Option.toNullable
              DestAccountName = dest.Name
              CreditSource = null
              DebitTarget = null }
        | Credit (account, CreditSource source) ->
            { Type = "Credit"
              SourceAccountId = Nullable()
              SourceAccountName = null
              DestAccountId = getAccountId account.Name |> Option.toNullable
              DestAccountName = account.Name
              CreditSource = source
              DebitTarget = null }
        | Debit (account, DebitTarget target) ->
            { Type = "Debit"
              SourceAccountId = getAccountId account.Name |> Option.toNullable
              SourceAccountName = account.Name
              DestAccountId = Nullable()
              DestAccountName = null
              CreditSource = null
              DebitTarget = target }

    { _id = ObjectId.GenerateNewId()
      SequenceNumber = seqNum
      Date = transaction.Date
      Sum = toMoneyDocument transaction.Sum
      Description = descDoc
      TextDescription = transaction.TextDescription |> Option.toObj }

let fromTransactionDocument
    (getAccount : ObjectId -> AccountType option)
    (getAccountByName : string -> AccountType option)
    (doc : TransactionDocument) : TransactionType option =

    let description =
        match doc.Description.Type with
        | "Transfer" ->
            let sourceOpt =
                if doc.Description.SourceAccountId.HasValue then
                    getAccount doc.Description.SourceAccountId.Value
                else
                    getAccountByName doc.Description.SourceAccountName
            let destOpt =
                if doc.Description.DestAccountId.HasValue then
                    getAccount doc.Description.DestAccountId.Value
                else
                    getAccountByName doc.Description.DestAccountName
            match sourceOpt, destOpt with
            | Some source, Some dest -> Some (Transfer (source, dest))
            | _ -> None

        | "Credit" ->
            let accountOpt =
                if doc.Description.DestAccountId.HasValue then
                    getAccount doc.Description.DestAccountId.Value
                else
                    getAccountByName doc.Description.DestAccountName
            match accountOpt with
            | Some account -> Some (Credit (account, CreditSource doc.Description.CreditSource))
            | None -> None

        | "Debit" ->
            let accountOpt =
                if doc.Description.SourceAccountId.HasValue then
                    getAccount doc.Description.SourceAccountId.Value
                else
                    getAccountByName doc.Description.SourceAccountName
            match accountOpt with
            | Some account -> Some (Debit (account, DebitTarget doc.Description.DebitTarget))
            | None -> None

        | _ -> None

    description |> Option.map (fun desc ->
        { Date = doc.Date
          Sum = fromMoneyDocument doc.Sum
          Description = desc
          TextDescription = Option.ofObj doc.TextDescription })
