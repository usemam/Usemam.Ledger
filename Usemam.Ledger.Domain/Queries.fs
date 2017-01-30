namespace Usemam.Ledger.Domain.Queries

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

type GetAllAccountsQuery() =
    interface IQuery<seq<AccountType>> with
        member this.run state =
            tryCatch (fun x -> x :> seq<AccountType>) state.accounts

type GetLastNTransactionsQuery(n : int) =
    interface IQuery<seq<TransactionType>> with
        member this.run state =
            result {
                let now = Clocks.machineClock ()
                let! total = tryCatch (fun x -> x |> Seq.length) state.transactions
                return state.transactions
                    |> Seq.sortBy (fun t -> now - t.Date)
                    |> Seq.take (min n total)
            }