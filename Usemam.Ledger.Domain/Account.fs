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

    let buildAccountNameMap (accounts : seq<AccountType>) =
        let getPrefixes (word : string) =
            Seq.unfold (
                fun i ->
                    if i > word.Length
                    then None
                    else Some(word.Substring(0, i), i + 1)
               ) 1
        let foldBack (prefixes : seq<seq<string>>) =
            let glue (s1 : string) (s2 : string) =
                if (String.IsNullOrEmpty s1) then s2
                else if (String.IsNullOrEmpty s2) then s2
                else s1 + " " + s2
            
            Seq.fold (fun acc e ->
                acc
                |> Seq.map (fun s ->
                    e |> Seq.map (fun p -> glue s p))
                |> Seq.concat
                ) (Seq.singleton String.Empty) prefixes
        let buildNameMap (names : seq<string>) =
            names
            |> Seq.map (fun name ->
                let words =
                    name.Split [|' '|]
                    |> Seq.filter (fun w -> String.IsNullOrEmpty w |> not)
                    |> Seq.toArray
                (name, words))
            |> Seq.map (fun (name, words) ->
                let prefixes =
                    words |> Seq.map (fun w -> w |> getPrefixes)
                let combinations = foldBack prefixes
                combinations |> Seq.map (fun c -> (c, name)))
            |> Seq.concat
            |> Seq.groupBy (fun (c, _) -> c)
            |> Seq.filter (fun (_, g) -> Seq.length g = 1)
            |> Seq.map (fun (_, g) -> g)
            |> Seq.concat
            |> Map.ofSeq
        accounts
        |> Seq.map (fun a -> a.Name)
        |> buildNameMap

    type IAccounts =
        inherit seq<AccountType>
        abstract getByName : string -> option<AccountType>
        abstract add : AccountType -> IAccounts
        abstract replace : AccountType -> IAccounts
        abstract remove : AccountType -> IAccounts

    type AccountsInMemory(accounts : seq<AccountType>, nameMap : Map<string, string>) =
        interface IAccounts with
            member this.GetEnumerator() = accounts.GetEnumerator()
            member this.GetEnumerator() =
                (this :> seq<AccountType>).GetEnumerator() :> System.Collections.IEnumerator
            member this.getByName name =
                accounts
                |> Seq.tryFind (fun a -> a.Name.ToLower() = name.ToLower())
            member this.add account =
                let newAccounts = accounts |> Seq.append [account]
                AccountsInMemory(newAccounts, buildAccountNameMap newAccounts)
                :> IAccounts
            member this.replace account =
                AccountsInMemory(
                    accounts |> Seq.filter (fun a -> a.Name <> account.Name) |> Seq.append [account],
                    nameMap)
                :> IAccounts
            member this.remove account =
                let newAccounts = accounts |> Seq.filter (fun a -> a.Name <> account.Name)
                AccountsInMemory(newAccounts, buildAccountNameMap newAccounts)
                :> IAccounts