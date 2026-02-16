module Usemam.Ledger.Import.Tests.ImportServiceTests

open System
open Xunit
open Usemam.Ledger.Import
open Usemam.Ledger.Domain

let private createAccount name =
    Account.create (fun () -> DateTimeOffset.Now) name (Money(Amount.zero, USD))

let private checkingAccount = createAccount "Checking"
let private savingsAccount = createAccount "Savings"

let private getAccount (name: string) : AccountType option =
    match name with
    | "Checking" -> Some checkingAccount
    | "Savings" -> Some savingsAccount
    | _ -> None

let private makeImportTransaction date amount desc category isCredit isTransfer transferAccount =
    {
        ImportTransaction.Date = date
        Amount = amount
        Description = desc
        Category = category
        IsCredit = isCredit
        IsTransfer = isTransfer
        TransferAccountName = transferAccount
    }

// buildTransactions tests

[<Fact>]
let ``buildTransactions creates credit transaction with CreditSource`` () =
    let date = DateTimeOffset(2025, 10, 15, 0, 0, 0, TimeSpan.Zero)
    let transactions = [| makeImportTransaction date 100.0m "Paycheck" "Salary" true false "" |]
    match ImportService.buildTransactions "Checking" transactions getAccount with
    | Ok results ->
        Assert.Equal(1, results.Length)
        let t = results.[0]
        Assert.Equal(date, t.Date)
        Assert.Equal(100.0m, t.Sum.Amount.Value)
        match t.Description with
        | Credit (_, CreditSource src) -> Assert.Equal("Salary", src)
        | _ -> Assert.True(false, "Expected Credit transaction")
    | Error errors -> Assert.True(false, sprintf "Expected Ok, got errors: %A" errors)

[<Fact>]
let ``buildTransactions creates debit transaction with DebitTarget`` () =
    let date = DateTimeOffset(2025, 10, 15, 0, 0, 0, TimeSpan.Zero)
    let transactions = [| makeImportTransaction date 50.0m "Grocery Store" "Groceries" false false "" |]
    match ImportService.buildTransactions "Checking" transactions getAccount with
    | Ok results ->
        Assert.Equal(1, results.Length)
        match results.[0].Description with
        | Debit (_, DebitTarget target) -> Assert.Equal("Groceries", target)
        | _ -> Assert.True(false, "Expected Debit transaction")
    | Error errors -> Assert.True(false, sprintf "Expected Ok, got errors: %A" errors)

[<Fact>]
let ``buildTransactions creates transfer with both accounts`` () =
    let date = DateTimeOffset(2025, 10, 15, 0, 0, 0, TimeSpan.Zero)
    let transactions = [| makeImportTransaction date 200.0m "Transfer to Savings" "Payment" false true "Savings" |]
    match ImportService.buildTransactions "Checking" transactions getAccount with
    | Ok results ->
        Assert.Equal(1, results.Length)
        match results.[0].Description with
        | Transfer (src, dst) ->
            Assert.Equal("Checking", src.Name)
            Assert.Equal("Savings", dst.Name)
        | _ -> Assert.True(false, "Expected Transfer transaction")
    | Error errors -> Assert.True(false, sprintf "Expected Ok, got errors: %A" errors)

[<Fact>]
let ``buildTransactions returns error for missing transfer account`` () =
    let date = DateTimeOffset(2025, 10, 15, 0, 0, 0, TimeSpan.Zero)
    let transactions = [| makeImportTransaction date 200.0m "Transfer" "Payment" false true "NonExistent" |]
    match ImportService.buildTransactions "Checking" transactions getAccount with
    | Error errors ->
        Assert.Equal(1, errors.Length)
        Assert.Contains("NonExistent", errors.[0])
    | Ok _ -> Assert.True(false, "Expected Error")

[<Fact>]
let ``buildTransactions returns error when main account not found`` () =
    let date = DateTimeOffset(2025, 10, 15, 0, 0, 0, TimeSpan.Zero)
    let transactions = [| makeImportTransaction date 50.0m "Test" "Misc" false false "" |]
    match ImportService.buildTransactions "NonExistent" transactions getAccount with
    | Error errors ->
        Assert.Equal(1, errors.Length)
        Assert.Contains("NonExistent", errors.[0])
    | Ok _ -> Assert.True(false, "Expected Error")

[<Fact>]
let ``buildTransactions mixed batch with one error returns Error`` () =
    let date = DateTimeOffset(2025, 10, 15, 0, 0, 0, TimeSpan.Zero)
    let transactions = [|
        makeImportTransaction date 50.0m "Grocery" "Groceries" false false ""
        makeImportTransaction date 200.0m "Transfer" "Payment" false true "NonExistent"
    |]
    match ImportService.buildTransactions "Checking" transactions getAccount with
    | Error errors ->
        Assert.Equal(1, errors.Length)
        Assert.Contains("NonExistent", errors.[0])
    | Ok _ -> Assert.True(false, "Expected Error for mixed batch with invalid transfer")

// summarizePreview tests

[<Fact>]
let ``summarizePreview counts credits debits and duplicates correctly`` () =
    let makeRaw isCredit = {
        Date = DateTimeOffset(2025, 10, 15, 0, 0, 0, TimeSpan.Zero)
        Amount = 50.0m
        Description = "Test"
        Category = None
        IsCredit = isCredit
    }
    let previews = [
        { Raw = makeRaw true; IsDuplicate = false; IsTransfer = false; Category = "Misc" }
        { Raw = makeRaw true; IsDuplicate = true; IsTransfer = false; Category = "Misc" }
        { Raw = makeRaw false; IsDuplicate = false; IsTransfer = false; Category = "Misc" }
        { Raw = makeRaw false; IsDuplicate = true; IsTransfer = false; Category = "Misc" }
        { Raw = makeRaw false; IsDuplicate = false; IsTransfer = true; Category = "Payment" }
    ]
    let summary = ImportService.summarizePreview previews
    Assert.Equal(5, summary.Total)
    Assert.Equal(2, summary.Credits)
    Assert.Equal(3, summary.Debits)
    Assert.Equal(2, summary.Duplicates)

[<Fact>]
let ``summarizePreview returns all zeros for empty list`` () =
    let summary = ImportService.summarizePreview []
    Assert.Equal(0, summary.Total)
    Assert.Equal(0, summary.Credits)
    Assert.Equal(0, summary.Debits)
    Assert.Equal(0, summary.Duplicates)
