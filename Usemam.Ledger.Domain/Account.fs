namespace Usemam.Ledger.Domain

type Currency = USD | RUR

type Amount = Amount of decimal

module AmountParser =

    let tryCreateAmount (d : decimal) =
        if d >= 0M
        then Some (Amount d)
        else None

    let parseAmount (s : string) =
        let (isParseSuccessful, d) = System.Decimal.TryParse(s)
        match isParseSuccessful with
        | true -> tryCreateAmount d
        | false -> None

type Money = Money of Amount * Currency

open System

type Account =
    {
        Created: DateTimeOffset
        Balance: Money
    }