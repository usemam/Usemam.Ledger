module Usemam.Ledger.API.Handlers

open System
open System.IO
open Microsoft.AspNetCore.Http
open Giraffe

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Transaction
open Usemam.Ledger.API.Dtos
open Usemam.Ledger.API.StateService

let private getStateService (ctx: HttpContext) =
    ctx.GetService<IStateService>()

let getAllAccounts : HttpHandler =
    fun next ctx ->
        let stateService = getStateService ctx
        let state = stateService.GetState()
        let accounts =
            state.accounts
            |> Seq.filter (fun a -> not a.IsClosed)
            |> Seq.map Mapping.toAccountDto
            |> Seq.toList
        json accounts next ctx

let getAccountByName (name: string) : HttpHandler =
    fun next ctx ->
        let stateService = getStateService ctx
        let state = stateService.GetState()
        match state.accounts.getByName name with
        | Some account -> json (Mapping.toAccountDto account) next ctx
        | None -> RequestErrors.notFound (text (sprintf "Account '%s' not found" name)) next ctx

let private getAccountNames (description: TransactionDescription) =
    match description with
    | Transfer (source, dest) -> [source.Name; dest.Name]
    | Credit (account, _) -> [account.Name]
    | Debit (account, _) -> [account.Name]

let getTransactionsForAccount (name: string) : HttpHandler =
    fun next ctx ->
        let stateService = getStateService ctx
        let state = stateService.GetState()
        match state.accounts.getByName name with
        | None -> RequestErrors.notFound (text (sprintf "Account '%s' not found" name)) next ctx
        | Some account ->
            let transactions =
                state.transactions
                |> Seq.filter (fun t -> getAccountNames t.Description |> List.exists (fun n -> n = account.Name))
                |> Seq.map Mapping.toTransactionDto
                |> Seq.toList
            json transactions next ctx

let getSpendingReport (year: int) : HttpHandler =
    fun next ctx ->
        let stateService = getStateService ctx
        let state = stateService.GetState()

        let startDate = DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero)
        let endDate = DateTimeOffset(year, 12, 31, 23, 59, 59, TimeSpan.Zero)

        let transactions =
            state.transactions.between startDate endDate
            |> Seq.filter (fun t -> not (Transaction.isTransfer t))
            |> Seq.toList

        // Collect all transactions with category and signed amount (positive for income, negative for expense)
        let allCategoryTransactions =
            transactions
            |> List.choose (fun t ->
                match t.Description with
                | Credit (_, CreditSource source) ->
                    Some (source, t.Date.Month, t.Sum.Amount.Value)  // positive for income
                | Debit (_, DebitTarget target) ->
                    Some (target, t.Date.Month, -t.Sum.Amount.Value)  // negative for expense
                | _ -> None)

        // Group by category name and calculate net monthly amounts
        let allCategories =
            allCategoryTransactions
            |> List.groupBy (fun (category, _, _) -> category)
            |> List.map (fun (category, items) ->
                let monthlyAmounts = Array.create 12 0m
                for (_, month, amount) in items do
                    monthlyAmounts.[month - 1] <- monthlyAmounts.[month - 1] + amount
                let yearTotal = Array.sum monthlyAmounts
                {
                    Category = category
                    MonthlyAmounts = monthlyAmounts
                    YearTotal = yearTotal
                } : CategorySpendingDto)
            |> List.sortBy (fun c -> c.Category)
            |> List.toArray

        let monthlyTotals = Array.create 12 0m
        for category in allCategories do
            for i in 0..11 do
                monthlyTotals.[i] <- monthlyTotals.[i] + category.MonthlyAmounts.[i]

        let yearlyNet = Array.sum monthlyTotals

        let report : SpendingReportDto = {
            Year = year
            Categories = allCategories
            MonthlyTotals = monthlyTotals
            YearlyNet = yearlyNet
        }

        json report next ctx

// Import handlers

let parseStatement : HttpHandler =
    fun next ctx ->
        task {
            let stateService = getStateService ctx
            let form = ctx.Request.Form

            // Get form fields
            let accountName = form.["accountName"].ToString()
            let formatStr = form.["format"].ToString()

            // Get uploaded file
            let files = form.Files
            if files.Count = 0 then
                return! RequestErrors.badRequest (text "No file uploaded") next ctx
            else
                let file = files.[0]

                // Save file to temp location
                let tempPath = Path.GetTempFileName()
                try
                    use stream = new FileStream(tempPath, FileMode.Create)
                    do! file.CopyToAsync(stream)
                    stream.Close()

                    // Validate account exists
                    match stateService.GetAccountByName accountName with
                    | None ->
                        return! RequestErrors.notFound (text (sprintf "Account '%s' not found" accountName)) next ctx
                    | Some _ ->

                        // Detect or parse format
                        let formatResult =
                            if String.IsNullOrWhiteSpace(formatStr) then
                                Usemam.Ledger.Import.FormatDetector.detectFromFile tempPath
                            else
                                Usemam.Ledger.Import.FormatDetector.parseFormatString formatStr

                        match formatResult with
                        | Error e ->
                            return! RequestErrors.badRequest (text e) next ctx
                        | Ok format ->

                            // Parse the CSV
                            match Usemam.Ledger.Import.Parsers.parseFile format tempPath with
                            | Error e ->
                                return! RequestErrors.badRequest (text e) next ctx
                            | Ok rawTransactions ->

                                // Check for duplicates
                                let state = stateService.GetState()
                                let existingTransactions = state.transactions |> Seq.toList

                                let duplicateChecks =
                                    rawTransactions
                                    |> List.map (fun raw ->
                                        let check = Usemam.Ledger.Import.Deduplication.checkForDuplicate existingTransactions raw 0.7
                                        (raw, check))

                                // Build response
                                let transactions =
                                    duplicateChecks
                                    |> List.map (fun (raw, check) ->
                                        let isDuplicate =
                                            match check with
                                            | Usemam.Ledger.Import.Deduplication.ExactDuplicate _
                                            | Usemam.Ledger.Import.Deduplication.PotentialDuplicate _ -> true
                                            | Usemam.Ledger.Import.Deduplication.Unique -> false
                                        {
                                            Date = raw.Date
                                            Amount = raw.Amount
                                            Description = raw.Description
                                            Category = "Misc"
                                            IsCredit = raw.IsCredit
                                            IsDuplicate = isDuplicate
                                        } : ParsedTransactionDto)
                                    |> List.toArray

                                let credits = transactions |> Array.filter (fun t -> t.IsCredit) |> Array.length
                                let debits = transactions |> Array.filter (fun t -> not t.IsCredit) |> Array.length
                                let duplicates = transactions |> Array.filter (fun t -> t.IsDuplicate) |> Array.length

                                let formatName =
                                    match format with
                                    | Usemam.Ledger.Import.BankFormat.Amex -> "amex"
                                    | Usemam.Ledger.Import.BankFormat.AppleCard -> "apple"
                                    | Usemam.Ledger.Import.BankFormat.Citi -> "citi"
                                    | Usemam.Ledger.Import.BankFormat.WellsFargo -> "wellsfargo"
                                    | Usemam.Ledger.Import.BankFormat.Discover -> "discover"

                                let result : ParseResultDto = {
                                    AccountName = accountName
                                    DetectedFormat = formatName
                                    Transactions = transactions
                                    Summary = {
                                        Total = transactions.Length
                                        Credits = credits
                                        Debits = debits
                                        Duplicates = duplicates
                                    }
                                }

                                return! json result next ctx
                finally
                    if File.Exists(tempPath) then
                        File.Delete(tempPath)
        }

let confirmImport : HttpHandler =
    fun next ctx ->
        task {
            let stateService = getStateService ctx
            let! importRequest = ctx.BindJsonAsync<ImportConfirmDto>()

            // Validate account exists
            match stateService.GetAccountByName importRequest.AccountName with
            | None ->
                return! RequestErrors.notFound (text (sprintf "Account '%s' not found" importRequest.AccountName)) next ctx
            | Some account ->

                // Convert DTOs to domain transactions
                let transactions =
                    importRequest.Transactions
                    |> Array.map (fun dto ->
                        let money = Money(Amount.create dto.Amount, USD)
                        if dto.IsCredit then
                            {
                                Date = dto.Date
                                Sum = money
                                Description = Credit (account, CreditSource dto.Category)
                                TextDescription = Some dto.Description
                            } : TransactionType
                        else
                            {
                                Date = dto.Date
                                Sum = money
                                Description = Debit (account, DebitTarget dto.Category)
                                TextDescription = Some dto.Description
                            } : TransactionType)
                    |> Array.toList

                // Add transactions
                match stateService.AddTransactions transactions with
                | Usemam.Ledger.Domain.Result.Success () ->
                    let result : ImportResultDto = {
                        Success = true
                        Imported = transactions.Length
                        Message = sprintf "Successfully imported %d transactions" transactions.Length
                    }
                    return! json result next ctx
                | Usemam.Ledger.Domain.Result.Failure msg ->
                    let result : ImportResultDto = {
                        Success = false
                        Imported = 0
                        Message = msg
                    }
                    return! ServerErrors.internalError (json result) next ctx
        }
