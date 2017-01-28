namespace Usemam.Ledger.Domain.Commands

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

open System

type AddAccountCommand(name, amount, credit) =
    interface ICommand with
        member this.run state =
            let balance = Money(amount, USD)
            Money(credit, USD)
            |> Account.createWithCredit Clocks.machineClock name balance
            |> state.addAccount
            |> Success
        member this.rollback state =
            result {
                let! account =
                    fromOption "Can't find account." (state.accounts.getByName name)
                return state.removeAccount account
            }

type TransferCommand(amount, source, dest, clock) =
    interface ICommand with
        member this.run state =
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
        member this.rollback state =
            let money = Money(amount, USD)
            let sourceAccount = state.accounts.getByName source
            let destAccount = state.accounts.getByName dest
            result {
                let! s = fromOption "Can't find source account." sourceAccount
                let! d = fromOption "Can't find destination account." destAccount
                let! rollbackTransaction =
                    Transaction.transferMoney d s money clock
                return
                    state
                    |> fun s -> s.popTransaction()
                    |> fun s -> s.replaceAccount (Transaction.getSourceAccount rollbackTransaction)
                    |> fun s -> s.replaceAccount (Transaction.getDestinationAccount rollbackTransaction)
            }

type CreditCommand(amount, source, dest, clock) =
    interface ICommand with
        member this.run state =
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
        member this.rollback state =
            let money = Money(amount, USD)
            let account = state.accounts.getByName dest
            result {
                let! d = fromOption "Can't find destination account." account
                return
                    state
                    |> fun s -> s.popTransaction()
                    |> fun s -> s.replaceAccount (Account.map (fun balance -> balance - money) d)
            }

type DebitCommand(amount, source, dest, clock) =
    interface ICommand with
        member this.run state =
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
        member this.rollback state =
            let money = Money(amount, USD)
            let account = state.accounts.getByName source
            result {
                let! a = fromOption "Can't find source account." account
                return
                    state
                    |> fun s -> s.popTransaction()
                    |> fun s -> s.replaceAccount (Account.map (fun balance -> balance + money) a)
            }