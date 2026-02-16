namespace Usemam.Ledger.Import

open System

type BankFormat =
    | Amex
    | AppleCard
    | Citi
    | WellsFargo
    | Discover

type RawTransaction = {
    Date: DateTimeOffset
    Amount: decimal
    Description: string
    Category: string option
    IsCredit: bool
}

type PreviewTransaction = {
    Raw: RawTransaction
    IsDuplicate: bool
    IsTransfer: bool
    Category: string
}

/// Input for building domain transactions from confirmed import data
type ImportTransaction = {
    Date: DateTimeOffset
    Amount: decimal
    Description: string
    Category: string
    IsCredit: bool
    IsTransfer: bool
    TransferAccountName: string
}

/// Summary of a parsed preview
type PreviewSummary = {
    Total: int
    Credits: int
    Debits: int
    Duplicates: int
}
