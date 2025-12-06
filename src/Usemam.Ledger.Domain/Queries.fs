namespace Usemam.Ledger.Domain.Queries

open System

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

type GetAllAccountsQuery() =
    interface IQuery<seq<AccountType>> with
        member this.run state =
            Success (state.accounts |> Seq.filter (fun a -> not a.IsClosed))

type GetAllCategoriesQuery() =
    interface IQuery<seq<string>> with
        member this.run state =
            let add (set : Set<string>) (t : TransactionType) =
                match t.Description with
                | Credit (_, source) -> source.ToString() |> set.Add
                | Debit (_, target) -> target.ToString() |> set.Add
                | _ -> failwith "Only credit and debit transactions supported."
            let aggregate () =
                state.transactions
                |> Seq.filter (fun t -> t |> Transaction.isTransfer |> not)
                |> Seq.fold add Set.empty
                |> seq
            tryCatch aggregate ()

type GetLastNTransactionsQuery(n : int, accountName : string) =
    interface IQuery<seq<TransactionType>> with
        member this.run state =
            let matchAccount t =
                String.IsNullOrEmpty accountName ||
                match t.Description with
                | Credit (account, _) -> account.matchName accountName
                | Debit (account, _) -> account.matchName accountName
                | Transfer (source, dest) -> source.matchName accountName || dest.matchName accountName
            result {
                let now = Clocks.machineClock ()
                let! total = tryCatch (fun x -> x |> Seq.filter matchAccount |> Seq.length) state.transactions
                return state.transactions
                    |> Seq.sortBy (fun t -> now - t.Date)
                    |> Seq.filter matchAccount
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
                |> Seq.filter (fun t -> t |> Transaction.isTransfer |> not)
                |> Seq.fold addOrUpdate Map.empty
            tryCatch aggregate ()