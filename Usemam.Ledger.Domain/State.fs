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
    member this.addTransaction transaction =
        State(accounts, transactions.add transaction)

type IQuery<'T> =
    abstract run : State -> Result<'T>

type ICommand =
    abstract run : State -> Result<State>