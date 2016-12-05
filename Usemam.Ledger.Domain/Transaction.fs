namespace Usemam.Ledger.Domain

type CreditSource = CreditSource of string

type DebitTarget = DebitTarget of string

type TransactionDescription =
    | Transfer of AccountType * AccountType
    | Credit of AccountType * CreditSource
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

    let transferMoney (source : AccountType) dest amount =
        match source.HasEnough amount with
        | true -> 
            let description = 
                (Account.map (fun x -> x - amount) source,
                 Account.map (fun x -> x + amount) dest)
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

module Credit =
    
    let putMoney account source amount =
        let description =
            (account |> Account.map (fun x -> x + amount), source)
            |> Credit
        let transaction =
            {
                Date = DateTimeOffset.Now
                Sum = amount
                Description = description
            }
        Success transaction

module Debit =
    
    let spendMoney (account : AccountType) target amount =
        match account.HasEnough amount with
        | true ->
            let description =
                (account |> Account.map (fun x -> x - amount), target)
                |> Debit
            let transaction =
                {
                    Date = DateTimeOffset.Now
                    Sum = amount
                    Description = description
                }
            Success transaction
        | false ->
            sprintf "Account '%O' doesn't have sufficient funds." account
            |> Failure