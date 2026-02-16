namespace Usemam.Ledger.Import

open System
open Usemam.Ledger.Domain

module ImportService =

    // Keywords for detecting payments from checking account to credit cards
    let private paymentKeywordsForChecking =
        ["CITI"; "AMEX"; "DISCOVER"; "AMERICAN EXPRESS"; "APPLECARD"; "APPLE CARD"; "GSBANK"]

    // Keywords for detecting payments on credit card statements
    let private paymentKeywordsForCreditCard =
        ["PAYMENT"; "ACH"; "ONLINE PAYMENT"; "AUTOPAY"; "WELLS FARGO"; "CHASE"; "BANK OF AMERICA"; "THANK YOU"]

    let detectTransfer (format: BankFormat) (raw: RawTransaction) : bool =
        let desc = raw.Description.ToUpperInvariant()
        match format with
        | WellsFargo ->
            // Checking account: debits to credit card companies are transfers
            not raw.IsCredit &&
            paymentKeywordsForChecking |> List.exists desc.Contains
        | Amex | AppleCard | Citi | Discover ->
            // Credit card: credits that are payments are transfers
            raw.IsCredit &&
            paymentKeywordsForCreditCard |> List.exists desc.Contains

    let formatToString (format: BankFormat) : string =
        match format with
        | Amex -> "amex"
        | AppleCard -> "apple"
        | Citi -> "citi"
        | WellsFargo -> "wellsfargo"
        | Discover -> "discover"

    let parseForPreview
        (filePath: string)
        (format: BankFormat option)
        (existingTransactions: TransactionType seq)
        (similarityThreshold: float)
        : Microsoft.FSharp.Core.Result<BankFormat * PreviewTransaction list, string> =

        // 1. Detect or use specified format
        let formatResult =
            match format with
            | Some f -> Ok f
            | None -> FormatDetector.detectFromFile filePath

        match formatResult with
        | Error e -> Error e
        | Ok detectedFormat ->
            // 2. Parse CSV
            match Parsers.parseFile detectedFormat filePath with
            | Error e -> Error e
            | Ok rawTransactions ->
                // 3. Build preview transactions with duplicate and transfer detection
                let previews =
                    rawTransactions
                    |> List.map (fun raw ->
                        let duplicateCheck = Deduplication.checkForDuplicate existingTransactions raw similarityThreshold
                        let isDuplicate =
                            match duplicateCheck with
                            | Deduplication.ExactDuplicate _
                            | Deduplication.PotentialDuplicate _ -> true
                            | Deduplication.Unique -> false
                        let isTransfer = detectTransfer detectedFormat raw
                        {
                            Raw = raw
                            IsDuplicate = isDuplicate
                            IsTransfer = isTransfer
                            Category = if isTransfer then "Payment" else "Misc"
                        })
                Ok (detectedFormat, previews)

    let summarizePreview (previews: PreviewTransaction list) : PreviewSummary =
        let arr = previews |> List.toArray
        {
            Total = arr.Length
            Credits = arr |> Array.filter (fun t -> t.Raw.IsCredit) |> Array.length
            Debits = arr |> Array.filter (fun t -> not t.Raw.IsCredit) |> Array.length
            Duplicates = arr |> Array.filter (fun t -> t.IsDuplicate) |> Array.length
        }

    let buildTransactions
        (accountName: string)
        (transactions: ImportTransaction array)
        (getAccount: string -> AccountType option)
        : Microsoft.FSharp.Core.Result<TransactionType list, string list> =

        match getAccount accountName with
        | None -> Error [sprintf "Account '%s' not found" accountName]
        | Some account ->
            let results =
                transactions
                |> Array.map (fun t ->
                    let money = Money(Amount.create t.Amount, USD)
                    if t.IsTransfer && not (String.IsNullOrWhiteSpace(t.TransferAccountName)) then
                        match getAccount t.TransferAccountName with
                        | None -> Error (sprintf "Transfer account '%s' not found" t.TransferAccountName)
                        | Some transferAccount ->
                            let (src, dst) =
                                if t.IsCredit then (transferAccount, account)
                                else (account, transferAccount)
                            Ok { Date = t.Date; Sum = money
                                 Description = Transfer (src, dst)
                                 TextDescription = Some t.Description }
                    elif t.IsCredit then
                        Ok { Date = t.Date; Sum = money
                             Description = Credit (account, CreditSource t.Category)
                             TextDescription = Some t.Description }
                    else
                        Ok { Date = t.Date; Sum = money
                             Description = Debit (account, DebitTarget t.Category)
                             TextDescription = Some t.Description })
            let errors = results |> Array.choose (function Error e -> Some e | _ -> None) |> Array.toList
            if not (List.isEmpty errors) then Error errors
            else Ok (results |> Array.choose (function Ok t -> Some t | _ -> None) |> Array.toList)
