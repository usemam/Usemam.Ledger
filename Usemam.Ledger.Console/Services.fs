module Usemam.Ledger.Console.Services

open Usemam.Ledger.Console.Command
open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result
open Usemam.Ledger.Domain.Queries

let query q (state : State) =
    let showAccounts () =
        result {
            let queryObj = GetAllAccountsQuery () :> IQuery<seq<AccountType>>
            let! queryResult = queryObj.run state
            return queryResult |> Seq.iteri (fun i a -> printfn "%i. %O" (i+1) a)
        }
    
    let showTransactions n  =
        result {
            let queryObj = GetLastNTransactionsQuery n :> IQuery<seq<TransactionType>>
            let! queryResult = queryObj.run state
            return queryResult |> Seq.iteri (fun i t -> printfn "%i. %O" (i+1) t)
        }

    result {
        let! _ =
            match q with
            | Accounts -> showAccounts ()
            | LastN n -> showTransactions n
        return state
    }

let addAccount name amount credit (state : State) =
    let balance = Money(amount, USD)
    Money(credit, USD)
    |> Account.createWithCredit Clocks.machineClock name balance
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
            |> fun s -> s.pushTransaction transaction
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
            |> fun s -> s.pushTransaction transaction
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
            |> fun s -> s.pushTransaction transaction
            |> fun s -> s.replaceAccount (Transaction.getSourceAccount transaction)
    }

let help (state : State) =
    result {
        let! _ = tryCatch Help.displayText ()
        return state
    }

let exit (state : State) =
    result {
        let! _ = Storage.saveState state
        return state
    }

let fromCommand (command : Command) =
    match command with
    | Show q -> query q
    | AddAccount (name, amount, credit) -> addAccount name amount credit
    | Command.Transfer (amount, On clock, From source, To dest) ->
        transfer amount source dest clock
    | Command.Credit (amount, On clock, From source, To dest) ->
        credit amount source dest clock
    | Command.Debit (amount, On clock, From source, To dest) ->
        debit amount source dest clock
    | Help -> help
    | Exit -> exit