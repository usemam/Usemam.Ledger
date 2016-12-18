﻿namespace Usemam.Ledger.Domain

open System

type Currency = USD

module Constants =
    
    let minAmount = 0.01M

type AmountType(d : decimal) =
    member this.Value = d
    static member (+) (a1 : AmountType, a2 : AmountType) =
        AmountType (a1.Value + a2.Value)
    static member (-) (a1 : AmountType, a2 : AmountType) =
        match (a1.Value - a2.Value), a1.Value = a2.Value with
        | _, true -> AmountType 0M
        | rest, false when rest >= Constants.minAmount -> AmountType rest
        | _ ->
            sprintf "Result amount cannot be less than %O." Constants.minAmount
            |> InvalidOperationException
            |> raise

module Amount =

    let tryCreate (d : decimal) =
        if d >= 0M
        then Some (AmountType d)
        else None

    let create (d : decimal) =
        match tryCreate d with
        | Some a -> a
        | None ->
            sprintf "Cannot create Amount with value %O." d
            |> InvalidOperationException
            |> raise

    let tryParse (s : string) =
        let (isParseSuccessful, d) = System.Decimal.TryParse(s)
        match isParseSuccessful with
        | true -> tryCreate d
        | false -> None

    let zero = create 0M

type Money(amount : AmountType, currency : Currency) =
    member this.Amount = amount
    member this.Currency = currency

    override this.ToString() =
        sprintf "%O %A" amount.Value currency
    override this.Equals (x : obj) =
        match x with
        | :? Money as other -> other.Amount.Value = amount.Value && other.Currency = currency
        | _ -> false
    override this.GetHashCode() =
        amount.Value.GetHashCode() &&& currency.GetHashCode()

    interface IComparable with
        member this.CompareTo(obj: obj) = 
            match obj with
            | :? Money as other ->
                amount.Value.CompareTo other.Amount.Value 
            | _ ->
                sprintf "Cannot perform comparison with %O." obj
                |> InvalidOperationException
                |> raise

    static member (+) (m1 : Money, m2 : Money) =
        Money(m1.Amount + m2.Amount, m1.Currency)
    static member (-) (m1 : Money, m2 : Money) =
        Money(m1.Amount - m2.Amount, m1.Currency)

type AccountType =
    {
        Name : string
        Created : DateTimeOffset
        Balance : Money
    }
    override this.ToString() =
        sprintf "%s %O" this.Name this.Balance
    member this.hasEnough money =
        match this.Balance >= money with
        | true -> Success ()
        | false ->
            sprintf "Account '%O' doesn't have sufficient funds." this
            |> Failure

module Account =
    
    let create name balance clock =
        {
            Name = name
            Balance = balance
            Created = clock()
        }

    let map f account =
        {
            Name = account.Name
            Created = account.Created
            Balance = f(account.Balance)
        }

    type IAccounts =
        inherit seq<AccountType>
        abstract getByName : string -> option<AccountType>
        abstract add : AccountType -> IAccounts
        abstract replace : AccountType -> IAccounts

    type AccountsInMemory(accounts : seq<AccountType>) =
        interface IAccounts with
            member this.GetEnumerator() = accounts.GetEnumerator()
            member this.GetEnumerator() =
                (this :> seq<AccountType>).GetEnumerator() :> System.Collections.IEnumerator
            member this.getByName name =
                accounts
                |> Seq.tryFind (fun a -> a.Name.ToLower() = name.ToLower())
            member this.add account =
                AccountsInMemory(accounts |> Seq.append [account])
                :> IAccounts
            member this.replace account =
                AccountsInMemory(accounts |> Seq.filter (fun a -> a.Name <> account.Name) |> Seq.append [account])
                :> IAccounts