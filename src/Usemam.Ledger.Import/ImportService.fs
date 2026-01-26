namespace Usemam.Ledger.Import

open System
open Usemam.Ledger.Domain

module ImportService =

    type private ImportResult<'T> = Microsoft.FSharp.Core.Result<'T, string>

    type ImportOptions = {
        FilePath: string
        AccountName: string
        Format: BankFormat option
        SimilarityThreshold: float
        DefaultCategory: string
        PreviewOnly: bool
    }

    let defaultOptions filePath accountName = {
        FilePath = filePath
        AccountName = accountName
        Format = None
        SimilarityThreshold = 0.7
        DefaultCategory = "Misc"
        PreviewOnly = false
    }

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
                            | Deduplication.ExactDuplicate _ | Deduplication.PotentialDuplicate _ -> true
                            | Deduplication.Unique -> false
                        let isTransfer = detectTransfer detectedFormat raw
                        {
                            Raw = raw
                            IsDuplicate = isDuplicate
                            IsTransfer = isTransfer
                            Category = if isTransfer then "Payment" else "Misc"
                        })
                Ok (detectedFormat, previews)

    let private detectTransferPayments
        (format: BankFormat)
        (transactions: RawTransaction list)
        : (RawTransaction * bool) list =

        transactions
        |> List.map (fun t -> (t, detectTransfer format t))

    let private toTransactionType
        (account: AccountType)
        (category: string)
        (textDescription: string)
        (raw: RawTransaction)
        : TransactionType =

        let money = Money(Amount.create raw.Amount, USD)

        if raw.IsCredit then
            {
                Date = raw.Date
                Sum = money
                Description = Credit (account, CreditSource category)
                TextDescription = Some textDescription
            }
        else
            {
                Date = raw.Date
                Sum = money
                Description = Debit (account, DebitTarget category)
                TextDescription = Some textDescription
            }

    let import
        (options: ImportOptions)
        (existingTransactions: TransactionType seq)
        (getAccount: string -> AccountType option)
        : ImportResult<ImportSummary> =

        // 1. Validate account exists
        match getAccount options.AccountName with
        | None ->
            Error (sprintf "Account '%s' not found" options.AccountName)
        | Some account ->

            // 2. Detect or use specified format
            let formatResult =
                match options.Format with
                | Some f -> Ok f
                | None -> FormatDetector.detectFromFile options.FilePath

            match formatResult with
            | Error e -> Error e
            | Ok format ->

                // 3. Parse the CSV file
                match Parsers.parseFile format options.FilePath with
                | Error e -> Error e
                | Ok rawTransactions when rawTransactions.IsEmpty ->
                    Ok {
                        TotalRows = 0
                        Imported = 0
                        Duplicates = 0
                        Skipped = 0
                        Transfers = 0
                        Results = []
                    }
                | Ok rawTransactions ->

                    // 4. Check for duplicates
                    let (unique, duplicates) =
                        Deduplication.filterUnique
                            existingTransactions
                            rawTransactions
                            options.SimilarityThreshold

                    // 5. Classify transactions (uses default category)
                    let classified =
                        Classification.classifyTransactions options.DefaultCategory unique

                    // 6. Detect transfer payments
                    let withTransfers = detectTransferPayments format unique

                    // 7. Build results
                    let duplicateResults =
                        duplicates
                        |> List.map (fun raw ->
                            let check =
                                Deduplication.checkForDuplicate
                                    existingTransactions
                                    raw
                                    options.SimilarityThreshold
                            match check with
                            | Deduplication.ExactDuplicate existing
                            | Deduplication.PotentialDuplicate (existing, _) ->
                                Duplicate (raw, existing)
                            | _ ->
                                Skipped (raw, "Unexpected state"))

                    let importedResults =
                        classified
                        |> List.map (fun (raw, category) ->
                            let transaction = toTransactionType account category raw.Description raw
                            Imported transaction)

                    let transferCount =
                        withTransfers
                        |> List.filter (fun (_, isTransfer) -> isTransfer)
                        |> List.length

                    Ok {
                        TotalRows = rawTransactions.Length
                        Imported = importedResults.Length
                        Duplicates = duplicateResults.Length
                        Skipped = 0
                        Transfers = transferCount
                        Results = importedResults @ duplicateResults
                    }

    let preview
        (options: ImportOptions)
        (existingTransactions: TransactionType seq)
        (getAccount: string -> AccountType option)
        : ImportResult<ImportSummary> =

        import { options with PreviewOnly = true } existingTransactions getAccount

    let getTransactionsToImport (summary: ImportSummary) : TransactionType list =
        summary.Results
        |> List.choose (fun r ->
            match r with
            | Imported t -> Some t
            | _ -> None)
