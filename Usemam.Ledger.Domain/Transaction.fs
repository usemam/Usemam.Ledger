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

module Transfer =
    
    type TransferResult =
        | Success of AccountType * AccountType
        | Failure of string

    let transferMoney (source : AccountType) dest (amount : Money) =
        match source.HasEnough amount with
        | true -> 
            Success
                (Account.map source (fun x -> x - amount),
                 Account.map dest (fun x -> x + amount))
        | false ->
            sprintf "Source account '%A' doesn't have sufficient funds." source.Balance
            |> Failure