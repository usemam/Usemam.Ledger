module Usemam.Ledger.Import.Tests.FormatDetectorTests

open Xunit
open Usemam.Ledger.Import

[<Fact>]
let ``detectFromHeader identifies AMEX format`` () =
    let header = "Date,Description,Card Member,Account #,Amount"
    let result = FormatDetector.detectFromHeader header
    Assert.Equal(Ok Amex, result)

[<Fact>]
let ``detectFromHeader identifies Apple Card format`` () =
    let header = "Transaction Date,Clearing Date,Description,Merchant,Category,Type,Amount (USD),Purchased By"
    let result = FormatDetector.detectFromHeader header
    Assert.Equal(Ok AppleCard, result)

[<Fact>]
let ``detectFromHeader identifies CITI format`` () =
    let header = "Status,Date,Description,Debit,Credit,Member Name"
    let result = FormatDetector.detectFromHeader header
    Assert.Equal(Ok Citi, result)

[<Fact>]
let ``detectFromHeader identifies Discover format`` () =
    let header = "Trans. Date,Post Date,Description,Amount,Category"
    let result = FormatDetector.detectFromHeader header
    Assert.Equal(Ok Discover, result)

[<Fact>]
let ``detectFromHeader returns error for unknown format`` () =
    let header = "Some,Unknown,Format"
    let result = FormatDetector.detectFromHeader header
    match result with
    | Error _ -> Assert.True(true)
    | Ok _ -> Assert.True(false, "Expected error for unknown format")

[<Fact>]
let ``parseFormatString parses amex`` () =
    Assert.Equal(Ok Amex, FormatDetector.parseFormatString "amex")
    Assert.Equal(Ok Amex, FormatDetector.parseFormatString "AMEX")

[<Fact>]
let ``parseFormatString parses apple`` () =
    Assert.Equal(Ok AppleCard, FormatDetector.parseFormatString "apple")
    Assert.Equal(Ok AppleCard, FormatDetector.parseFormatString "applecard")

[<Fact>]
let ``parseFormatString parses citi`` () =
    Assert.Equal(Ok Citi, FormatDetector.parseFormatString "citi")

[<Fact>]
let ``parseFormatString parses discover`` () =
    Assert.Equal(Ok Discover, FormatDetector.parseFormatString "discover")
    Assert.Equal(Ok Discover, FormatDetector.parseFormatString "disco")

[<Fact>]
let ``parseFormatString parses wellsfargo`` () =
    Assert.Equal(Ok WellsFargo, FormatDetector.parseFormatString "wellsfargo")
    Assert.Equal(Ok WellsFargo, FormatDetector.parseFormatString "wf")
