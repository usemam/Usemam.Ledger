namespace Usemam.Ledger.Domain

open System

type Currency = USD | RUR

module Constants =
    
    let minAmount = 0.01M

type AmountType(d : decimal) =
    member this.Value = d
    static member (+) (a1 : AmountType, a2 : AmountType) =
        AmountType (a1.Value + a2.Value)
    static member (-) (a1 : AmountType, a2 : AmountType) =
        match a1.Value - a2.Value with
        | rest when rest >= Constants.minAmount -> AmountType rest
        | _ ->
            sprintf "Subtracted amount cannot be less than %O." Constants.minAmount
            |> InvalidOperationException
            |> raise

module Amount =

    let tryCreate (d : decimal) =
        if d >= 0M
        then Some (AmountType d)
        else None

    let tryParse (s : string) =
        let (isParseSuccessful, d) = System.Decimal.TryParse(s)
        match isParseSuccessful with
        | true -> tryCreate d
        | false -> None

type Money(amount : AmountType, currency : Currency) =
    member this.Amount = amount
    member this.Currency = currency
    // todo: obviously, addition and subtraction are not correct
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
    member this.HasEnough money = this.Balance = money

module Account =
    
    let map account f =
        {
            Name = account.Name
            Created = account.Created
            Balance = f(account.Balance)
        }