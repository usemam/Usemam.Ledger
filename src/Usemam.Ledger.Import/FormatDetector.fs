namespace Usemam.Ledger.Import

open System
open System.IO

module FormatDetector =

    type private FResult<'T> = Microsoft.FSharp.Core.Result<'T, string>

    let private containsAll (keywords: string list) (line: string) =
        let lower = line.ToLowerInvariant()
        keywords |> List.forall (fun k -> lower.Contains(k))

    let detectFromHeader (headerLine: string) : FResult<BankFormat> =
        let line = headerLine.ToLowerInvariant()

        if containsAll ["card member"; "account #"] line then
            Ok Amex
        elif containsAll ["clearing date"; "purchased by"] line then
            Ok AppleCard
        elif containsAll ["member name"; "status"] line then
            Ok Citi
        elif containsAll ["trans. date"; "post date"] line then
            Ok Discover
        else
            Error "Unable to detect bank format from headers"

    let detectFromFile (filePath: string) : FResult<BankFormat> =
        try
            use reader = new StreamReader(filePath)
            let firstLine = reader.ReadLine()

            if String.IsNullOrWhiteSpace(firstLine) then
                Error "File is empty"
            else
                match detectFromHeader firstLine with
                | Ok format -> Ok format
                | Error _ ->
                    // Check if it looks like WellsFargo (no headers, quoted dates)
                    if firstLine.StartsWith("\"") && firstLine.Contains(",") then
                        Ok WellsFargo
                    else
                        Error (sprintf "Unable to detect format. First line: %s" firstLine)
        with
        | ex -> Error (sprintf "Error reading file: %s" ex.Message)

    let formatToString (format: BankFormat) : string =
        match format with
        | Amex -> "American Express"
        | AppleCard -> "Apple Card"
        | Citi -> "Citi"
        | WellsFargo -> "Wells Fargo"
        | Discover -> "Discover"

    let parseFormatString (s: string) : FResult<BankFormat> =
        match s.ToLowerInvariant() with
        | "amex" | "americanexpress" -> Ok Amex
        | "apple" | "applecard" -> Ok AppleCard
        | "citi" -> Ok Citi
        | "wellsfargo" | "wf" | "wells" -> Ok WellsFargo
        | "discover" | "disco" -> Ok Discover
        | _ -> Error (sprintf "Unknown format: %s" s)
