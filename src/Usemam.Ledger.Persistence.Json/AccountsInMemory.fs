namespace Usemam.Ledger.Persistence.Json

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Account

type AccountsInMemory(accounts : seq<AccountType>) =
    interface IAccounts with
        member this.GetEnumerator() = accounts.GetEnumerator()
        member this.GetEnumerator() =
            (this :> seq<AccountType>).GetEnumerator() :> System.Collections.IEnumerator
        member this.getByName name =
            accounts
            |> Seq.tryFind (fun a -> a.matchName name)
        member this.add account =
            let newAccounts = accounts |> Seq.append [account]
            AccountsInMemory(newAccounts) :> IAccounts
        member this.replace account =
            AccountsInMemory(
                accounts |> Seq.filter (fun a -> a.Name <> account.Name) |> Seq.append [account])
            :> IAccounts
        member this.remove account =
            let newAccounts = accounts |> Seq.filter (fun a -> a.Name <> account.Name)
            AccountsInMemory(newAccounts)
            :> IAccounts
