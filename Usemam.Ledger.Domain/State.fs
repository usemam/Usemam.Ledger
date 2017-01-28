namespace Usemam.Ledger.Domain

open Usemam.Ledger.Domain.Account
open Usemam.Ledger.Domain.Transaction

type State(accounts : IAccounts, transactions : ITransactions) =
    member this.accounts = accounts
    member this.transactions = transactions
    member this.addAccount account =
        State(accounts.add account, transactions)
    member this.replaceAccount account =
        State(accounts.replace account, transactions)
    member this.removeAccount account =
        State(accounts.remove account, transactions)
    member this.pushTransaction transaction =
        State(accounts, transactions.push transaction)
    member this.popTransaction () =
        State(accounts, transactions.pop ())

type IQuery<'T> =
    abstract run : State -> Result<'T>

type ICommand =
    abstract run : State -> Result<State>
    abstract rollback : State -> Result<State>