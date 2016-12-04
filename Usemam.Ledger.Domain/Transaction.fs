namespace Usemam.Ledger.Domain

type CreditSource = CreditSource of string

type DebitTarget = DebitTarget of string

type TransactionDescription =
    | Transfer of AccountType * AccountType
    | Credit of CreditSource * AccountType
    | Debit of AccountType * DebitTarget

open System

type Transaction =
    {
        Date : DateTimeOffset
        Sum : Money
        Description : TransactionDescription
    }

type TransactionResult =
        | Success of Transaction
        | Failure of string

module Transfer =

    let transferMoney (source : AccountType) dest (amount : Money) =
        match source.HasEnough amount with
        | true -> 
            let description = 
                (Account.map source (fun x -> x - amount),
                 Account.map dest (fun x -> x + amount))
                |> Transfer
            let transaction =
                {
                    Date = DateTimeOffset.Now
                    Sum = amount
                    Description = description
                }
            Success transaction
        | false ->
            sprintf "Source account '%O' doesn't have sufficient funds." source
            |> Failure