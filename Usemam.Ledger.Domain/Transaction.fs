namespace Usemam.Ledger.Domain

type CreditSource = CreditSource of string

type DebitTarget = DebitTarget of string

type TransactionDescription =
    | Transfer of Account * Account
    | Credit of CreditSource * Account
    | Debit of Account * DebitTarget

open System

type Transaction =
    {
        Date : DateTimeOffset
        Sum : Money
        Description : TransactionDescription
    }