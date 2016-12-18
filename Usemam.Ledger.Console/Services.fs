module Usemam.Ledger.Console.Services

open System

open Usemam.Ledger.Console.ColorPrint
open Usemam.Ledger.Console.Parser

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

let dummy state =
    cprintfn ConsoleColor.Yellow "Doing nothing."
    state |> Success

let query q (state : State) =
    let showAccounts () =
        state.accounts
        |> Seq.iteri (fun i a -> printfn "%i. %O" (i+1) a)

    match q with
    | Accounts -> showAccounts ()
    Success state

let addAccount name amount (state : State) =
    let balance = Money(amount, USD)
    Account.create name balance Clocks.machineClock
    |> state.addAccount
    |> Success

let transfer amount source dest (state : State) =
    let money = Money(amount, USD)
    let sourceAccount = state.accounts.getByName source
    let destAccount = state.accounts.getByName dest
    result {
        let! s = fromOption "Can't find source account." sourceAccount
        let! d = fromOption "Can't find destination account." destAccount
        let! transaction =
            Transfer.transferMoney s d money
        return
            state
            |> fun s -> s.addTransaction transaction
            |> fun s -> s.replaceAccount (Transaction.getSourceAccount transaction)
            |> fun s -> s.replaceAccount (Transaction.getDestinationAccount transaction)
    }

let credit amount source dest (state : State) =
    let money = Money(amount, USD)
    let category = CreditSource source
    let account = state.accounts.getByName dest
    result {
        let! d = fromOption "Can't find destination account." account
        let! transaction = Credit.putMoney d category money
        return
            state
            |> fun s -> s.addTransaction transaction
            |> fun s -> s.replaceAccount (Transaction.getDestinationAccount transaction)
    }

let debit amount source dest (state : State) =
    let money = Money(amount, USD)
    let category = DebitTarget dest
    let account = state.accounts.getByName source
    result {
        let! a = fromOption "Can't find source account." account
        let! transaction = Debit.spendMoney a category money
        return
            state
            |> fun s -> s.addTransaction transaction
            |> fun s -> s.replaceAccount (Transaction.getSourceAccount transaction)
    }

let fromCommand (command : Command) : Service =
    match command with
    | Show q -> query q
    | AddAccount (name, amount) -> addAccount name amount
    | Parser.Transfer (amount, From source, To dest) ->
        transfer amount source dest
    | Parser.Credit (amount, From source, To dest) ->
        credit amount source dest
    | Parser.Debit (amount, From source, To dest) ->
        debit amount source dest
    | _ -> dummy