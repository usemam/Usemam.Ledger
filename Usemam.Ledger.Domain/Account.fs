namespace Usemam.Ledger.Domain

open System

type AccountType =
    {
        Name : string
        Created : DateTimeOffset
        Balance : Money
        Credit : Money
    }
    override this.ToString() =
        sprintf "%s - %O - created on %O" this.Name this.Balance this.Created
    member this.hasEnough money =
        match this.Balance + this.Credit >= money with
        | true -> Success ()
        | false ->
            sprintf "Account '%O' doesn't have sufficient funds." this
            |> Failure

module Account =

    let createWithCredit clock name balance credit =
        {
            Name = name
            Balance = balance
            Credit = credit
            Created = clock()
        }
    
    let create clock name (balance : Money) =
        Money(Amount.zero, balance.Currency)
        |> createWithCredit clock name balance

    let map f account =
        {
            Name = account.Name
            Created = account.Created
            Credit = account.Credit
            Balance = f(account.Balance)
        }

    type IAccounts =
        inherit seq<AccountType>
        abstract getByName : string -> option<AccountType>
        abstract add : AccountType -> IAccounts
        abstract replace : AccountType -> IAccounts
        abstract remove : AccountType -> IAccounts

    type AccountsInMemory(accounts : seq<AccountType>) =
        interface IAccounts with
            member this.GetEnumerator() = accounts.GetEnumerator()
            member this.GetEnumerator() =
                (this :> seq<AccountType>).GetEnumerator() :> System.Collections.IEnumerator
            member this.getByName name =
                accounts
                |> Seq.tryFind (fun a ->
                    name.ToLowerInvariant()
                    |> a.Name.ToLowerInvariant().StartsWith)
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