module Usemam.Ledger.Console.Services

open Usemam.Ledger.Console.Command
open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result
open Usemam.Ledger.Domain.Queries
open Usemam.Ledger.Domain.Commands

let query q (tracker : CommandTracker) =
    let showAccounts () =
        result {
            let queryObj = GetAllAccountsQuery () :> IQuery<seq<AccountType>>
            let! queryResult = queryObj.run tracker.state
            return queryResult |> Seq.iteri (fun i a -> printfn "%i. %O" (i+1) a)
        }
    
    let showTransactions n  =
        result {
            let queryObj = GetLastNTransactionsQuery n :> IQuery<seq<TransactionType>>
            let! queryResult = queryObj.run tracker.state
            return queryResult |> Seq.iteri (fun i t -> printfn "%i. %O" (i+1) t)
        }

    let showTotals min max =
        result {
            let queryObj = GetTotalsQuery (min, max) :> IQuery<Map<string, Money>>
            let! queryResult = queryObj.run tracker.state
            return
                queryResult
                |> Seq.sortBy (fun x -> x.Value)
                |> Seq.iteri (fun i x -> printfn "%i. %s - %O" (i+1) x.Key x.Value)
        }

    result {
        let! _ =
            match q with
            | Accounts -> showAccounts ()
            | LastN n -> showTransactions n
            | Total (min, max) -> showTotals min max
        return tracker
    }

let addAccount name amount credit (tracker : CommandTracker) =
    AddAccountCommand (name, amount, credit)
    |> tracker.run

let setCreditLimit name amount (tracker : CommandTracker) =
    SetCreditLimitCommand(name, amount)
    |> tracker.run

let transfer amount source dest clock (tracker : CommandTracker) =
    TransferCommand (amount, source, dest, clock)
    |> tracker.run

let credit amount source dest clock (tracker : CommandTracker) =
    CreditCommand (amount, source, dest, clock)
    |> tracker.run

let debit amount source dest clock (tracker : CommandTracker) =
    DebitCommand (amount, source, dest, clock)
    |> tracker.run

let undo (tracker : CommandTracker) = tracker.undo ()

let redo (tracker : CommandTracker) = tracker.redo ()

let help (tracker : CommandTracker) =
    result {
        let! _ = tryCatch Help.displayText ()
        return tracker
    }

let exit (tracker : CommandTracker) =
    result {
        let! _ = Storage.saveState tracker.state
        return tracker
    }

let fromCommand (command : Command) =
    match command with
    | Show q -> query q
    | AddAccount (name, amount, credit) -> addAccount name amount credit
    | SetCreditLimit (name, amount) -> setCreditLimit name amount
    | Command.Transfer (amount, On clock, From source, To dest) ->
        transfer amount source dest clock
    | Command.Credit (amount, On clock, From source, To dest) ->
        credit amount source dest clock
    | Command.Debit (amount, On clock, From source, To dest) ->
        debit amount source dest clock
    | Undo -> undo
    | Redo -> redo
    | Help -> help
    | Exit -> exit