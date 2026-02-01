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

type ImportResult =
    | Imported of Usemam.Ledger.Domain.TransactionType
    | Duplicate of RawTransaction * existing: Usemam.Ledger.Domain.TransactionType
    | Skipped of RawTransaction * reason: string
    | Transfer of source: RawTransaction * dest: RawTransaction

type ImportSummary = {
    TotalRows: int
    Imported: int
    Duplicates: int
    Skipped: int
    Transfers: int
    Results: ImportResult list
}

type PreviewTransaction = {
    Raw: RawTransaction
    IsDuplicate: bool
    IsTransfer: bool
    Category: string
}
