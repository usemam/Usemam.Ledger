module Usemam.Ledger.Import.Tests.ParsersTests

open System
open System.IO
open Xunit
open Usemam.Ledger.Import

let private createTempFile (content: string) =
    let path = Path.GetTempFileName()
    File.WriteAllText(path, content)
    path

let private cleanup path =
    if File.Exists(path) then File.Delete(path)

[<Fact>]
let ``Amex parser parses valid CSV`` () =
    let content = """Date,Description,Card Member,Account #,Amount
10/13/2025,AMAZON MARKEPLACE,JOHN DOE,-12049,60.30
10/13/2025,MOBILE PAYMENT - THANK YOU,JOHN DOE,-15077,-677.29"""
    let path = createTempFile content
    try
        let result = Parsers.Amex.parse path
        match result with
        | Ok transactions ->
            Assert.Equal(2, transactions.Length)

            let first = transactions.[0]
            Assert.Equal(60.30m, first.Amount)
            Assert.False(first.IsCredit)
            Assert.Equal("AMAZON MARKEPLACE", first.Description)

            let second = transactions.[1]
            Assert.Equal(677.29m, second.Amount)
            Assert.True(second.IsCredit)
        | Error e -> Assert.True(false, e)
    finally
        cleanup path

[<Fact>]
let ``Apple Card parser parses valid CSV with categories`` () =
    let content = """Transaction Date,Clearing Date,Description,Merchant,Category,Type,Amount (USD),Purchased By
09/27/2025,09/29/2025,"SAFEWAY #1794 1850 PRAIRIE",Safeway,Grocery,Purchase,74.69,John Doe
09/19/2025,09/20/2025,"HUCKBERRY INC RETURN",Huckberry,Credit,Credit,-71.12,John Doe"""
    let path = createTempFile content
    try
        let result = Parsers.AppleCard.parse path
        match result with
        | Ok transactions ->
            Assert.Equal(2, transactions.Length)

            let first = transactions.[0]
            Assert.Equal(74.69m, first.Amount)
            Assert.False(first.IsCredit)
            Assert.Equal(Some "Grocery", first.Category)

            let second = transactions.[1]
            Assert.Equal(71.12m, second.Amount)
            Assert.True(second.IsCredit)
            Assert.Equal(Some "Credit", second.Category)
        | Error e -> Assert.True(false, e)
    finally
        cleanup path

[<Fact>]
let ``Citi parser parses valid CSV with separate debit/credit columns`` () =
    let content = """Status,Date,Description,Debit,Credit,Member Name
Cleared,10/18/2025,"PAYMENT THANK YOU",,-979.80,JOHN DOE
Cleared,10/15/2025,"TTOBONGEE CHICKEN",4.96,,JOHN DOE"""
    let path = createTempFile content
    try
        let result = Parsers.Citi.parse path
        match result with
        | Ok transactions ->
            Assert.Equal(2, transactions.Length)

            let first = transactions.[0]
            Assert.Equal(979.80m, first.Amount)
            Assert.True(first.IsCredit)

            let second = transactions.[1]
            Assert.Equal(4.96m, second.Amount)
            Assert.False(second.IsCredit)
        | Error e -> Assert.True(false, e)
    finally
        cleanup path

[<Fact>]
let ``WellsFargo parser parses valid CSV without headers`` () =
    let line1 = "\"10/17/2025\",\"2366.07\",\"*\",\"\",\"EMPLOYER PAYROLL DEPOSIT\""
    let line2 = "\"10/16/2025\",\"-474.60\",\"*\",\"\",\"PURCHASE AUTHORIZED\""
    let content = line1 + Environment.NewLine + line2
    let path = createTempFile content
    try
        let result = Parsers.WellsFargo.parse path
        match result with
        | Ok transactions ->
            Assert.Equal(2, transactions.Length)

            let first = transactions.[0]
            Assert.Equal(2366.07m, first.Amount)
            Assert.True(first.IsCredit)

            let second = transactions.[1]
            Assert.Equal(474.60m, second.Amount)
            Assert.False(second.IsCredit)
        | Error e -> Assert.True(false, e)
    finally
        cleanup path

[<Fact>]
let ``Discover parser parses valid CSV with categories`` () =
    let content = """Trans. Date,Post Date,Description,Amount,Category
10/15/2025,10/15/2025,"SACRAMENTO MUNICIPAL",127.52,Services
09/27/2025,09/27/2025,"INTERNET PAYMENT - THANK YOU",-180.00,Payments and Credits"""
    let path = createTempFile content
    try
        let result = Parsers.Discover.parse path
        match result with
        | Ok transactions ->
            Assert.Equal(2, transactions.Length)

            let first = transactions.[0]
            Assert.Equal(127.52m, first.Amount)
            Assert.False(first.IsCredit)
            Assert.Equal(Some "Services", first.Category)

            let second = transactions.[1]
            Assert.Equal(180.00m, second.Amount)
            Assert.True(second.IsCredit)
        | Error e -> Assert.True(false, e)
    finally
        cleanup path

[<Fact>]
let ``parseFile dispatches to correct parser`` () =
    let content = """Date,Description,Card Member,Account #,Amount
10/13/2025,TEST TRANSACTION,JOHN DOE,-12009,50.00"""
    let path = createTempFile content
    try
        let result = Parsers.parseFile Amex path
        match result with
        | Ok transactions ->
            Assert.Equal(1, transactions.Length)
            Assert.Equal(50.00m, transactions.[0].Amount)
        | Error e -> Assert.True(false, e)
    finally
        cleanup path
