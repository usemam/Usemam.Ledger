module Usemam.Ledger.Console.Services

open Usemam.Ledger.Console.Command
open Usemam.Ledger.Console.ColorPrint
open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result
open Usemam.Ledger.Domain.Queries
open Usemam.Ledger.Domain.Commands

let commandPrompt = tryCatch printEntryArrow
let followWithCommandPrompt (r : Result<CommandTracker, string>) =
    result {
        let! _ = commandPrompt ()
        return! r
    }

let query q (tracker : CommandTracker) =
    let showAccounts () =
        result {
            let queryObj = GetAllAccountsQuery () :> IQuery<seq<AccountType>>
            let! queryResult = queryObj.run tracker.state
            return queryResult |> Seq.iteri (fun i a -> printfn "%i. %O" (i+1) a)
        }
    
    let showTransactions n accountName =
        result {
            let queryObj = GetLastNTransactionsQuery (n, accountName) :> IQuery<seq<TransactionType>>
            let! queryResult = queryObj.run tracker.state
            return queryResult |> Seq.iteri (fun i t -> printfn "%i. %O" (i+1) t)
        }

    let showTotals min max =
        result {
            let queryObj = GetTotalsQuery (min, max) :> IQuery<Map<string, Money>>
            let! queryResult = queryObj.run tracker.state
            queryResult
            |> Seq.sortBy (fun x -> x.Value)
            |> Seq.iteri (fun i x -> printfn "%i. %s - %O" (i+1) x.Key x.Value)
            printfn "--------------------------------"
            |> ignore
            return
                queryResult
                |> Seq.sumBy (fun x -> x.Value)
                |> printfn "Total - %O"
        }

    result {
        let! _ =
            match q with
            | Accounts -> showAccounts ()
            | LastN (n, accountName)  -> showTransactions n accountName
            | Total (min, max) -> showTotals min max
        let! _ = commandPrompt ()
        return tracker
    }

let addAccount name amount credit (tracker : CommandTracker) =
    AddAccountCommand (name, amount, credit)
    |> tracker.run
    |> followWithCommandPrompt

let setCreditLimit name amount (tracker : CommandTracker) =
    SetCreditLimitCommand(name, amount)
    |> tracker.run
    |> followWithCommandPrompt

let closeAccount name (tracker : CommandTracker) =
    CloseAccountCommand (name)
    |> tracker.run
    |> followWithCommandPrompt

let transfer amount source dest clock (tracker : CommandTracker) =
    TransferCommand (amount, source, dest, clock)
    |> tracker.run
    |> followWithCommandPrompt

let credit amount source dest clock (tracker : CommandTracker) =
    CreditCommand (amount, source, dest, clock)
    |> tracker.run
    |> followWithCommandPrompt

let debit amount source dest clock (tracker : CommandTracker) =
    DebitCommand (amount, source, dest, clock)
    |> tracker.run
    |> followWithCommandPrompt

let undo (tracker : CommandTracker) =
    tracker.undo ()
    |> followWithCommandPrompt

let redo (tracker : CommandTracker) =
    tracker.redo ()
    |> followWithCommandPrompt

let help (tracker : CommandTracker) =
    result {
        let! _ = tryCatch Help.displayText ()
        let! _ = commandPrompt ()
        return tracker
    }

let exit (tracker : CommandTracker) =
    result {
        let! _ = Storage.saveState tracker.state
        return tracker
    }

let moveCaret (positionDelta : int) (tracker : CommandTracker) =
    result {
        let! _ = tryCatch Input.moveCaret positionDelta
        return tracker
    }

let fromCommand (command : Command) =
    match command with
    | Show q -> query q
    | AddAccount (name, amount, credit) -> addAccount name amount credit
    | SetCreditLimit (name, amount) -> setCreditLimit name amount
    | CloseAccount (name) -> closeAccount name
    | Command.Transfer (amount, On clock, From source, To dest) ->
        transfer amount source dest clock
    | Command.Credit (amount, On clock, From source, To dest) ->
        credit amount source dest clock
    | Command.Debit (amount, On clock, From source, To dest) ->
        debit amount source dest clock
    | ArrowLeft -> moveCaret -1
    | ArrowRight -> moveCaret 1
    | Undo -> undo
    | Redo -> redo
    | Help -> help
    | Exit -> exit