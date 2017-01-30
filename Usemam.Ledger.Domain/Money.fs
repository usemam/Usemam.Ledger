namespace Usemam.Ledger.Domain

open System

type Currency = USD

module Constants =
    
    let minAmount = 0.01M

type AmountType =
    { Value : decimal }
    static member (+) (a1 : AmountType, a2 : AmountType) =
        { Value = (a1.Value + a2.Value) }
    static member (-) (a1 : AmountType, a2 : AmountType) =
        match (a1.Value - a2.Value), a1.Value = a2.Value with
        | _, true -> { Value = 0M }
        | rest, false when Math.Abs(rest) >= Constants.minAmount -> { Value = rest }
        | _ ->
            sprintf "Result amount cannot be less than %O." Constants.minAmount
            |> InvalidOperationException
            |> raise

module Amount =

    let tryCreate (d : decimal) =
        if d = 0M || Math.Abs(d) >= Constants.minAmount
        then Some { Value = d }
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