namespace Usemam.Ledger.Import

open System
open System.Globalization
open System.IO
open System.Text.RegularExpressions

module Parsers =

    type private ParseResult<'T> = Microsoft.FSharp.Core.Result<'T, string>

    let private dateFormats = [| "MM/dd/yyyy"; "M/d/yyyy"; "yyyy-MM-dd" |]

    let private parseDate (s: string) : DateTimeOffset option =
        let cleanedDate = s.Trim().Trim('"')
        match DateTimeOffset.TryParseExact(
                cleanedDate,
                dateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal) with
        | true, date -> Some date
        | false, _ -> None

    let private parseDecimal (s: string) : decimal option =
        let cleaned = s.Trim().Trim('"').Replace("$", "").Replace(",", "")
        match Decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture) with
        | true, d -> Some d
        | false, _ -> None

    let private splitCsvLine (line: string) : string[] =
        // Handle quoted fields with commas
        let pattern = ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"
        Regex.Split(line, pattern)
        |> Array.map (fun s -> s.Trim().Trim('"'))

    // AMEX Parser
    // Headers: Date,Description,Card Member,Account #,Amount
    // Positive = purchase/debit, Negative = payment/credit
    module Amex =
        let parse (filePath: string) : ParseResult<RawTransaction list> =
            try
                let lines = File.ReadAllLines(filePath)
                if lines.Length < 2 then
                    Ok []
                else
                    lines
                    |> Array.skip 1 // Skip header
                    |> Array.choose (fun line ->
                        let parts = splitCsvLine line
                        if parts.Length >= 5 then
                            match parseDate parts.[0], parseDecimal parts.[4] with
                            | Some date, Some amount ->
                                Some {
                                    Date = date
                                    Amount = abs amount
                                    Description = parts.[1]
                                    Category = None
                                    IsCredit = amount < 0m
                                }
                            | _ -> None
                        else None)
                    |> Array.toList
                    |> Ok
            with
            | ex -> Error (sprintf "Error parsing AMEX file: %s" ex.Message)

    // Apple Card Parser
    // Headers: Transaction Date,Clearing Date,Description,Merchant,Category,Type,Amount (USD),Purchased By
    // Type column determines credit/debit
    module AppleCard =
        let parse (filePath: string) : ParseResult<RawTransaction list> =
            try
                let lines = File.ReadAllLines(filePath)
                if lines.Length < 2 then
                    Ok []
                else
                    lines
                    |> Array.skip 1 // Skip header
                    |> Array.choose (fun line ->
                        let parts = splitCsvLine line
                        if parts.Length >= 7 then
                            match parseDate parts.[0], parseDecimal parts.[6] with
                            | Some date, Some amount ->
                                let txType = parts.[5].ToLowerInvariant()
                                let isCredit =
                                    txType = "credit" ||
                                    txType = "payment" ||
                                    amount < 0m
                                Some {
                                    Date = date
                                    Amount = abs amount
                                    Description = parts.[2]
                                    Category = Some parts.[4]
                                    IsCredit = isCredit
                                }
                            | _ -> None
                        else None)
                    |> Array.toList
                    |> Ok
            with
            | ex -> Error (sprintf "Error parsing Apple Card file: %s" ex.Message)

    // CITI Parser
    // Headers: Status,Date,Description,Debit,Credit,Member Name
    // Separate Debit and Credit columns
    module Citi =
        let parse (filePath: string) : ParseResult<RawTransaction list> =
            try
                let lines = File.ReadAllLines(filePath)
                if lines.Length < 2 then
                    Ok []
                else
                    lines
                    |> Array.skip 1 // Skip header
                    |> Array.choose (fun line ->
                        let parts = splitCsvLine line
                        if parts.Length >= 5 then
                            match parseDate parts.[1] with
                            | Some date ->
                                let debitAmount = parseDecimal parts.[3]
                                let creditAmount = parseDecimal parts.[4]
                                match debitAmount, creditAmount with
                                | Some d, _ when d > 0m ->
                                    Some {
                                        Date = date
                                        Amount = d
                                        Description = parts.[2]
                                        Category = None
                                        IsCredit = false
                                    }
                                | _, Some c when c <> 0m ->
                                    Some {
                                        Date = date
                                        Amount = abs c
                                        Description = parts.[2]
                                        Category = None
                                        IsCredit = true
                                    }
                                | _ -> None
                            | None -> None
                        else None)
                    |> Array.toList
                    |> Ok
            with
            | ex -> Error (sprintf "Error parsing CITI file: %s" ex.Message)

    // WellsFargo Parser (Checking)
    // No headers, columns: "Date","Amount","*","","Description"
    // Positive = deposit/credit, Negative = withdrawal/debit
    module WellsFargo =
        let parse (filePath: string) : ParseResult<RawTransaction list> =
            try
                let lines = File.ReadAllLines(filePath)
                lines
                |> Array.choose (fun line ->
                    let parts = splitCsvLine line
                    if parts.Length >= 5 then
                        match parseDate parts.[0], parseDecimal parts.[1] with
                        | Some date, Some amount ->
                            Some {
                                Date = date
                                Amount = abs amount
                                Description = parts.[4]
                                Category = None
                                IsCredit = amount > 0m
                            }
                        | _ -> None
                    else None)
                |> Array.toList
                |> Ok
            with
            | ex -> Error (sprintf "Error parsing WellsFargo file: %s" ex.Message)

    // Discover Parser
    // Headers: Trans. Date,Post Date,Description,Amount,Category
    // Positive = purchase/debit, Negative = payment/credit
    module Discover =
        let parse (filePath: string) : ParseResult<RawTransaction list> =
            try
                let lines = File.ReadAllLines(filePath)
                if lines.Length < 2 then
                    Ok []
                else
                    lines
                    |> Array.skip 1 // Skip header
                    |> Array.choose (fun line ->
                        let parts = splitCsvLine line
                        if parts.Length >= 5 then
                            match parseDate parts.[0], parseDecimal parts.[3] with
                            | Some date, Some amount ->
                                Some {
                                    Date = date
                                    Amount = abs amount
                                    Description = parts.[2]
                                    Category = Some parts.[4]
                                    IsCredit = amount < 0m
                                }
                            | _ -> None
                        else None)
                    |> Array.toList
                    |> Ok
            with
            | ex -> Error (sprintf "Error parsing Discover file: %s" ex.Message)

    let parseFile (format: BankFormat) (filePath: string) : ParseResult<RawTransaction list> =
        match format with
        | Amex -> Amex.parse filePath
        | AppleCard -> AppleCard.parse filePath
        | Citi -> Citi.parse filePath
        | WellsFargo -> WellsFargo.parse filePath
        | Discover -> Discover.parse filePath
