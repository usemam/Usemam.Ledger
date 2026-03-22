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

type SetCreditLimitCommand(name, amount) =
    let mutable oldLimit : Money = Money(Amount.create 0M, USD)
    interface ICommand with
        member this.run state =
            let newLimit = Money(amount, USD)
            let account = state.accounts.getByName name
            result {
                let! a = fromOption (sprintf "Can't find account '%s'" name) account
                oldLimit <- a.Credit
                let newAccount = Account.setCreditLimit a newLimit
                return
                    state |> fun s -> s.replaceAccount newAccount
            }
        member this.rollback state =
            let account = state.accounts.getByName name
            result {
                let! a = fromOption (sprintf "Can't find account '%s'" name) account
                let oldAccount = Account.setCreditLimit a oldLimit
                return
                    state |> fun s -> s.replaceAccount oldAccount
            }

type CloseAccountCommand(name) =
    interface ICommand with
        member this.run state =
            let account = state.accounts.getByName name
            result {
                let! a = fromOption (sprintf "Can't find account '%s'" name) account
                let newAccount = Account.setIsClosed a true
                return
                    state |> fun s -> s.replaceAccount newAccount
            }
        member this.rollback state =
            let account = state.accounts.getByName name
            result {
                let! a = fromOption (sprintf "Can't find account '%s'" name) account
                let newAccount = Account.setIsClosed a false
                return
                    state |> fun s -> s.replaceAccount newAccount
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

type RecalculateBalancesCommand() =
    let mutable oldBalances : Map<string, Money> = Map.empty

    interface ICommand with
        member _.run state =
            let deltas =
                state.transactions
                |> Seq.fold (fun (map: Map<string, Money>) t ->
                    match t.Description with
                    | Credit (acc, _) ->
                        map |> Map.change acc.Name (fun b -> Some (defaultArg b Money.Zero + t.Sum))
                    | Debit (acc, _) ->
                        map |> Map.change acc.Name (fun b -> Some (defaultArg b Money.Zero - t.Sum))
                    | Transfer (src, dst) ->
                        map
                        |> Map.change src.Name (fun b -> Some (defaultArg b Money.Zero - t.Sum))
                        |> Map.change dst.Name (fun b -> Some (defaultArg b Money.Zero + t.Sum))
                ) Map.empty
            oldBalances <- state.accounts |> Seq.map (fun a -> a.Name, a.Balance) |> Map.ofSeq
            let newState =
                state.accounts
                |> Seq.fold (fun (s: State) account ->
                    match Map.tryFind account.Name deltas with
                    | Some newBalance ->
                        printfn "%s: %O -> %O" account.Name account.Balance newBalance
                        s.replaceAccount (Account.map (fun _ -> newBalance) account)
                    | None -> s
                ) state
            Success newState

        member _.rollback state =
            let newState =
                state.accounts
                |> Seq.fold (fun (s: State) account ->
                    match Map.tryFind account.Name oldBalances with
                    | Some oldBalance ->
                        s.replaceAccount (Account.map (fun _ -> oldBalance) account)
                    | None -> s
                ) state
            Success newState