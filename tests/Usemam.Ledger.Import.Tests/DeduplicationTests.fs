module Usemam.Ledger.Import.Tests.DeduplicationTests

open System
open Xunit
open Usemam.Ledger.Import
open Usemam.Ledger.Domain

[<Fact>]
let ``jaccardSimilarity returns 1.0 for identical strings`` () =
    let result = Deduplication.jaccardSimilarity "hello world" "hello world"
    Assert.Equal(1.0, result)

[<Fact>]
let ``jaccardSimilarity returns 0.0 for completely different strings`` () =
    let result = Deduplication.jaccardSimilarity "abc def" "xyz uvw"
    Assert.Equal(0.0, result)

[<Fact>]
let ``jaccardSimilarity returns value between 0 and 1 for partial match`` () =
    let result = Deduplication.jaccardSimilarity "hello world test" "hello world different"
    Assert.True(result > 0.0 && result < 1.0)

[<Fact>]
let ``jaccardSimilarity ignores numbers and symbols`` () =
    let result = Deduplication.jaccardSimilarity "AMAZON #123" "AMAZON #456"
    Assert.Equal(1.0, result)

[<Fact>]
let ``checkForDuplicate returns Unique when no matching transactions`` () =
    let existing : TransactionType seq = Seq.empty
    let raw = {
        Date = DateTimeOffset.Now
        Amount = 50.0m
        Description = "Test transaction"
        Category = None
        IsCredit = false
    }
    let result = Deduplication.checkForDuplicate existing raw 0.7
    Assert.Equal(Deduplication.Unique, result)

[<Fact>]
let ``checkForDuplicate returns ExactDuplicate for matching transaction`` () =
    let existingTransaction = {
        Date = DateTimeOffset(2025, 10, 15, 0, 0, 0, TimeSpan.Zero)
        Sum = Money(Amount.create 50.0m, USD)
        Description = Debit (Account.create (fun () -> DateTimeOffset.Now) "Checking" (Money(Amount.zero, USD)), DebitTarget "Grocery")
        TextDescription = Some "SAFEWAY GROCERY STORE"
    }
    let existing = seq { existingTransaction }
    let raw = {
        Date = DateTimeOffset(2025, 10, 15, 0, 0, 0, TimeSpan.Zero)
        Amount = 50.0m
        Description = "SAFEWAY GROCERY STORE"
        Category = None
        IsCredit = false
    }
    let result = Deduplication.checkForDuplicate existing raw 0.7
    match result with
    | Deduplication.ExactDuplicate _ -> Assert.True(true)
    | _ -> Assert.True(false, "Expected ExactDuplicate")

[<Fact>]
let ``checkForDuplicate returns Unique when amount differs`` () =
    let existingTransaction = {
        Date = DateTimeOffset(2025, 10, 15, 0, 0, 0, TimeSpan.Zero)
        Sum = Money(Amount.create 100.0m, USD)
        Description = Debit (Account.create (fun () -> DateTimeOffset.Now) "Checking" (Money(Amount.zero, USD)), DebitTarget "Grocery")
        TextDescription = Some "SAFEWAY GROCERY STORE"
    }
    let existing = seq { existingTransaction }
    let raw = {
        Date = DateTimeOffset(2025, 10, 15, 0, 0, 0, TimeSpan.Zero)
        Amount = 50.0m
        Description = "SAFEWAY GROCERY STORE"
        Category = None
        IsCredit = false
    }
    let result = Deduplication.checkForDuplicate existing raw 0.7
    Assert.Equal(Deduplication.Unique, result)

[<Fact>]
let ``checkForDuplicate returns Unique when date differs`` () =
    let existingTransaction = {
        Date = DateTimeOffset(2025, 10, 14, 0, 0, 0, TimeSpan.Zero)
        Sum = Money(Amount.create 50.0m, USD)
        Description = Debit (Account.create (fun () -> DateTimeOffset.Now) "Checking" (Money(Amount.zero, USD)), DebitTarget "Grocery")
        TextDescription = Some "SAFEWAY GROCERY STORE"
    }
    let existing = seq { existingTransaction }
    let raw = {
        Date = DateTimeOffset(2025, 10, 15, 0, 0, 0, TimeSpan.Zero)
        Amount = 50.0m
        Description = "SAFEWAY GROCERY STORE"
        Category = None
        IsCredit = false
    }
    let result = Deduplication.checkForDuplicate existing raw 0.7
    Assert.Equal(Deduplication.Unique, result)

[<Fact>]
let ``filterUnique separates unique from duplicates`` () =
    let existingTransaction = {
        Date = DateTimeOffset(2025, 10, 15, 0, 0, 0, TimeSpan.Zero)
        Sum = Money(Amount.create 50.0m, USD)
        Description = Debit (Account.create (fun () -> DateTimeOffset.Now) "Checking" (Money(Amount.zero, USD)), DebitTarget "Grocery")
        TextDescription = Some "SAFEWAY GROCERY STORE"
    }
    let existing = seq { existingTransaction }

    let rawDuplicate = {
        Date = DateTimeOffset(2025, 10, 15, 0, 0, 0, TimeSpan.Zero)
        Amount = 50.0m
        Description = "SAFEWAY GROCERY STORE"
        Category = None
        IsCredit = false
    }
    let rawUnique = {
        Date = DateTimeOffset(2025, 10, 16, 0, 0, 0, TimeSpan.Zero)
        Amount = 75.0m
        Description = "COSTCO WHOLESALE"
        Category = None
        IsCredit = false
    }

    let (unique, duplicates) = Deduplication.filterUnique existing [rawDuplicate; rawUnique] 0.7

    Assert.Equal(1, unique.Length)
    Assert.Equal(1, duplicates.Length)
    Assert.Equal(75.0m, unique.[0].Amount)
    Assert.Equal(50.0m, duplicates.[0].Amount)
