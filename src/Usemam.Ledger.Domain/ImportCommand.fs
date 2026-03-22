namespace Usemam.Ledger.Domain.Commands

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

type ImportCommand(transactions: TransactionType list) =
    let mutable appliedCount = 0

    interface ICommand with
        member _.run state =
            let newState =
                transactions
                |> List.fold (fun (s: State) t ->
                    let s' = s.pushTransaction t
                    match t.Description with
                    | Credit (acc, _) ->
                        match s'.accounts.getByName acc.Name with
                        | Some current -> s'.replaceAccount (Account.map (fun b -> b + t.Sum) current)
                        | None -> s'
                    | Debit (acc, _) ->
                        match s'.accounts.getByName acc.Name with
                        | Some current -> s'.replaceAccount (Account.map (fun b -> b - t.Sum) current)
                        | None -> s'
                    | Transfer (src, dst) ->
                        let s'' =
                            match s'.accounts.getByName src.Name with
                            | Some currentSrc -> s'.replaceAccount (Account.map (fun b -> b - t.Sum) currentSrc)
                            | None -> s'
                        match s''.accounts.getByName dst.Name with
                        | Some currentDst -> s''.replaceAccount (Account.map (fun b -> b + t.Sum) currentDst)
                        | None -> s'') state
            appliedCount <- transactions.Length
            Success newState

        member _.rollback state =
            let mutable s = state
            for _ in 1..appliedCount do
                s <- s.popTransaction()
            for t in List.rev (transactions |> List.truncate appliedCount) do
                s <-
                    match t.Description with
                    | Credit (acc, _) ->
                        match s.accounts.getByName acc.Name with
                        | Some current -> s.replaceAccount (Account.map (fun b -> b - t.Sum) current)
                        | None -> s
                    | Debit (acc, _) ->
                        match s.accounts.getByName acc.Name with
                        | Some current -> s.replaceAccount (Account.map (fun b -> b + t.Sum) current)
                        | None -> s
                    | Transfer (src, dst) ->
                        let s' =
                            match s.accounts.getByName src.Name with
                            | Some currentSrc -> s.replaceAccount (Account.map (fun b -> b + t.Sum) currentSrc)
                            | None -> s
                        match s'.accounts.getByName dst.Name with
                        | Some currentDst -> s'.replaceAccount (Account.map (fun b -> b - t.Sum) currentDst)
                        | None -> s'
            Success s
