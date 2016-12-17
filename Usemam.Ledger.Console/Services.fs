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
        let! d = fromOption "Can't find dest account." destAccount
        let! transaction =
            Transfer.transferMoney s d money
        return state.addTransaction transaction
    }

let fromCommand (command : Command) : Service =
    match command with
    | Show q -> query q
    | AddAccount (name, amount) -> addAccount name amount
    | Parser.Transfer (amount, From source, To dest) ->
        transfer amount source dest
    | _ -> dummy