namespace Usemam.Ledger.Persistence.Json

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Transaction

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
