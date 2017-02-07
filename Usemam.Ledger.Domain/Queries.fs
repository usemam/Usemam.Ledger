namespace Usemam.Ledger.Domain.Queries

open System

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

type GetTotalsQuery(startDate : DateTimeOffset, endDate : DateTimeOffset) =
    interface IQuery<Map<string, Money>> with
        member this.run state =
            let addOrUpdate (map : Map<string, Money>) (t : TransactionType) =
                let (name, updateAmount) =
                    match t.Description with
                    | Credit (_, source) -> (source.ToString(), fun a -> a + t.Sum)
                    | Debit (_, target) -> (target.ToString(), fun a -> a - t.Sum)
                    | _ -> failwith "Only credit and debit transactions supported."
                let (map', amount) =
                    match map.TryFind name with
                    | None -> (map, Money(Amount.zero, USD) |> updateAmount)
                    | Some a -> (map.Remove name, a |> updateAmount)
                map'.Add (name, amount)
            let aggregate () =
                state.transactions.between startDate endDate
                |> Seq.filter (fun t ->
                    match t.Description with
                    | Credit _ -> true
                    | Debit _ -> true
                    | _ -> false)
                |> Seq.fold addOrUpdate Map.empty
            tryCatch aggregate ()