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

    let private detectTransferPayments
        (transactions: RawTransaction list)
        : (RawTransaction * RawTransaction option) list =

        let paymentKeywords = ["CITI"; "AMEX"; "DISCOVER"; "AMERICAN EXPRESS"; "APPLECARD"; "APPLE CARD"; "GSBANK"]

        let isPaymentFromChecking (t: RawTransaction) =
            not t.IsCredit &&
            paymentKeywords |> List.exists (fun kw -> t.Description.ToUpperInvariant().Contains(kw))

        let credits = transactions |> List.filter (fun t -> t.IsCredit)

        transactions
        |> List.map (fun t ->
            if isPaymentFromChecking t then
                let matchingCredit =
                    credits
                    |> List.tryFind (fun c ->
                        c.Amount = t.Amount &&
                        abs (t.Date - c.Date).Days <= 3)
                (t, matchingCredit)
            else
                (t, None))

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
                    let withTransfers = detectTransferPayments unique

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
                        |> List.filter (fun (_, matching) -> matching.IsSome)
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
