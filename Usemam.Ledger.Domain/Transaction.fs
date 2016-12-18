namespace Usemam.Ledger.Domain

type CreditSource = CreditSource of string

type DebitTarget = DebitTarget of string

type TransactionDescription =
    | Transfer of AccountType * AccountType // first account is source, second is a destination
    | Credit of AccountType * CreditSource
    | Debit of AccountType * DebitTarget

open System

type TransactionType =
    {
        Date : DateTimeOffset
        Sum : Money
        Description : TransactionDescription
    }

module Transaction =
    
    type ITransactions =
        inherit seq<TransactionType>
        abstract between : DateTimeOffset -> DateTimeOffset -> seq<TransactionType>
        abstract add : TransactionType -> ITransactions

    type TransactionsInMemory(transactions : seq<TransactionType>) =
        interface ITransactions with
            member this.GetEnumerator() = transactions.GetEnumerator()
            member this.GetEnumerator() =
                (this :> seq<TransactionType>).GetEnumerator() :> System.Collections.IEnumerator
            member this.between min max =
                transactions
                |> Seq.filter (fun t -> min <= t.Date && t.Date <= max)
            member this.add transaction =
                TransactionsInMemory(transactions |> Seq.append [transaction])
                :> ITransactions

    let create amount description =
        {
            Date = DateTimeOffset.Now
            Sum = amount
            Description = description
        }

    let getSourceAccount (transaction : TransactionType) =
        transaction.Description
        |> fun d ->
            match d with
            | Transfer (s, _) -> s
            | Debit (s, _) -> s
            | _ -> failwith "Operation is not supported."

    let getDestinationAccount (transaction : TransactionType) =
        transaction.Description
        |> fun d ->
            match d with
            | Transfer (_, a) -> a
            | Credit (a, _) -> a
            | _ -> failwith "Operation is not supported."

open Usemam.Ledger.Domain.Result

module Transfer =

    let transferMoney (source : AccountType) dest amount =
        let result = new ResultBuilder()
        result {
            let! _ = source.hasEnough amount
            let! sourceDebited =
                tryCatch (Account.map (fun x -> x - amount)) source
            let! destCredited =
                tryCatch (Account.map (fun x -> x + amount)) dest
            return
                (sourceDebited, destCredited)
                |> Transfer
                |> Transaction.create amount
        }

module Credit =
    
    let putMoney account source amount =
        (account |> Account.map (fun x -> x + amount), source)
        |> Credit
        |> Transaction.create amount
        |> Success

module Debit =
    
    let spendMoney (account : AccountType) target amount =
        let result = new ResultBuilder()
        result {
            let! _ = account.hasEnough amount
            let! accountDebited =
                tryCatch (Account.map (fun x -> x - amount)) account
            return
                (accountDebited, target)
                |> Debit
                |> Transaction.create amount
        }