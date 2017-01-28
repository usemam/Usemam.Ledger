namespace Usemam.Ledger.Domain

type CreditSource =
    | CreditSource of string
    override this.ToString() =
        match this with | CreditSource s -> s

type DebitTarget =
    | DebitTarget of string
    override this.ToString() =
        match this with | DebitTarget s -> s

type TransactionDescription =
    | Transfer of AccountType * AccountType // first account is source, second is a destination
    | Credit of AccountType * CreditSource
    | Debit of AccountType * DebitTarget
    override this.ToString() =
        match this with
        | Transfer (a1, a2) -> sprintf "transferred from %s to %s" a1.Name a2.Name
        | Credit (a, s) -> sprintf "credited from %s to %O" a.Name s
        | Debit (a, t) -> sprintf "spent from %s on %O" a.Name t

open System

type TransactionType =
    {
        Date : DateTimeOffset
        Sum : Money
        Description : TransactionDescription
    }
    override this.ToString() =
        sprintf "%O - %O - %O" this.Date this.Sum this.Description

module Transaction =
    
    type ITransactions =
        inherit seq<TransactionType>
        abstract between : DateTimeOffset -> DateTimeOffset -> seq<TransactionType>
        abstract push : TransactionType -> ITransactions
        abstract pop : unit -> ITransactions

    type TransactionsInMemory(transactions : TransactionType list) =
        interface ITransactions with
            member this.GetEnumerator() = (transactions :> seq<TransactionType>).GetEnumerator()
            member this.GetEnumerator() =
                (this :> seq<TransactionType>).GetEnumerator() :> System.Collections.IEnumerator
            member this.between min max =
                transactions
                |> Seq.filter (fun t -> min <= t.Date && t.Date <= max)
            member this.push transaction =
                TransactionsInMemory(transaction :: transactions)
                :> ITransactions
            member this.pop () = 
                TransactionsInMemory(transactions.Tail)
                :> ITransactions

    let create clock amount description =
        {
            Date = clock()
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

    let transferMoney (source : AccountType) dest amount clock =
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
                |> create clock amount
        }

    let putMoney account source amount clock =
        (account |> Account.map (fun x -> x + amount), source)
        |> Credit
        |> create clock amount
        |> Success

    let spendMoney (account : AccountType) target amount clock =
        let result = new ResultBuilder()
        result {
            let! _ = account.hasEnough amount
            let! accountDebited =
                tryCatch (Account.map (fun x -> x - amount)) account
            return
                (accountDebited, target)
                |> Debit
                |> create clock amount
        }