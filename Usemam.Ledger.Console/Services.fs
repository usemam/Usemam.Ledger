module Usemam.Ledger.Console.Services

open System

open Usemam.Ledger.Console.ColorPrint
open Usemam.Ledger.Console.Command
open Usemam.Ledger.Console.Parser

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

let query q (state : State) =
    let showAccounts () =
        state.accounts
        |> Seq.iteri (fun i a -> printfn "%i. %O" (i+1) a)
    
    let showTransactions period =
        let min, max = Dates.BoundariesIn period
        state.transactions.between min max
        |> Seq.sortBy (fun t -> t.Date)
        |> Seq.iteri (fun i t -> printfn "%i. %O" (i+1) t)

    match q with
    | Accounts -> showAccounts ()
    | Today -> Clocks.machineClock |> Dates.today |> showTransactions
    | Query.LastWeek -> Clocks.machineClock |> Dates.lastWeek |> showTransactions
    Success state

let addAccount name amount (state : State) =
    let balance = Money(amount, USD)
    Account.create Clocks.machineClock name balance
    |> state.addAccount
    |> Success

let transfer amount source dest clock (state : State) =
    let money = Money(amount, USD)
    let sourceAccount = state.accounts.getByName source
    let destAccount = state.accounts.getByName dest
    result {
        let! s = fromOption "Can't find source account." sourceAccount
        let! d = fromOption "Can't find destination account." destAccount
        let! transaction =
            Transaction.transferMoney s d money clock
        return
            state
            |> fun s -> s.addTransaction transaction
            |> fun s -> s.replaceAccount (Transaction.getSourceAccount transaction)
            |> fun s -> s.replaceAccount (Transaction.getDestinationAccount transaction)
    }

let credit amount source dest clock (state : State) =
    let money = Money(amount, USD)
    let category = CreditSource source
    let account = state.accounts.getByName dest
    result {
        let! d = fromOption "Can't find destination account." account
        let! transaction = Transaction.putMoney d category money clock
        return
            state
            |> fun s -> s.addTransaction transaction
            |> fun s -> s.replaceAccount (Transaction.getDestinationAccount transaction)
    }

let debit amount source dest clock (state : State) =
    let money = Money(amount, USD)
    let category = DebitTarget dest
    let account = state.accounts.getByName source
    result {
        let! a = fromOption "Can't find source account." account
        let! transaction = Transaction.spendMoney a category money clock
        return
            state
            |> fun s -> s.addTransaction transaction
            |> fun s -> s.replaceAccount (Transaction.getSourceAccount transaction)
    }

let exit (state : State) =
    result {
        let! _ = Storage.saveState state
        return state
    }

let fromCommand (command : Command) : Service =
    match command with
    | Show q -> query q
    | AddAccount (name, amount) -> addAccount name amount
    | Command.Transfer (amount, On clock, From source, To dest) ->
        transfer amount source dest clock
    | Command.Credit (amount, On clock, From source, To dest) ->
        credit amount source dest clock
    | Command.Debit (amount, On clock, From source, To dest) ->
        debit amount source dest clock
    | Exit -> exit