module Usemam.Ledger.API.Dtos

open System
open Usemam.Ledger.Domain

[<CLIMutable>]
type MoneyDto =
    {
        Amount: decimal
        Currency: string
    }

[<CLIMutable>]
type AccountDto =
    {
        Name: string
        IsClosed: bool
        Created: DateTimeOffset
        Balance: MoneyDto
        CreditLimit: MoneyDto
    }

[<CLIMutable>]
type TransactionDto =
    {
        Date: DateTimeOffset
        Amount: MoneyDto
        Type: string
        SourceAccount: string option
        DestinationAccount: string option
        CreditSource: string option
        DebitTarget: string option
        Description: string option
    }

module Mapping =
    let toMoneyDto (money: Money) : MoneyDto =
        {
            Amount = money.Amount.Value
            Currency =
                match money.Currency with
                | USD -> "USD"
        }

    let toAccountDto (account: AccountType) : AccountDto =
        {
            Name = account.Name
            IsClosed = account.IsClosed
            Created = account.Created
            Balance = toMoneyDto account.Balance
            CreditLimit = toMoneyDto account.Credit
        }

    let toTransactionDto (transaction: TransactionType) : TransactionDto =
        let (transactionType, sourceAccount, destAccount, creditSource, debitTarget) =
            match transaction.Description with
            | Transfer (source, dest) ->
                ("Transfer", Some source.Name, Some dest.Name, None, None)
            | Credit (account, CreditSource source) ->
                ("Credit", None, Some account.Name, Some source, None)
            | Debit (account, DebitTarget target) ->
                ("Debit", Some account.Name, None, None, Some target)
        {
            Date = transaction.Date
            Amount = toMoneyDto transaction.Sum
            Type = transactionType
            SourceAccount = sourceAccount
            DestinationAccount = destAccount
            CreditSource = creditSource
            DebitTarget = debitTarget
            Description = transaction.TextDescription
        }

[<CLIMutable>]
type CategorySpendingDto =
    {
        Category: string
        MonthlyAmounts: decimal array
        YearTotal: decimal
    }

[<CLIMutable>]
type SpendingReportDto =
    {
        Year: int
        Categories: CategorySpendingDto array
        MonthlyTotals: decimal array
        YearlyNet: decimal
    }
