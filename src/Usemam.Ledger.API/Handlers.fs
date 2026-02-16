module Usemam.Ledger.API.Handlers

open System
open System.IO
open Microsoft.AspNetCore.Http
open Giraffe

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Transaction
open Usemam.Ledger.Import
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

let private tryParseInt (s: string) (defaultValue: int) =
    match System.Int32.TryParse(s) with
    | true, v when v >= 0 -> v
    | _ -> defaultValue

let getTransactionsForAccount (name: string) : HttpHandler =
    fun next ctx ->
        let stateService = getStateService ctx
        let state = stateService.GetState()
        match state.accounts.getByName name with
        | None -> RequestErrors.notFound (text (sprintf "Account '%s' not found" name)) next ctx
        | Some account ->
            let skip = ctx.Request.Query.["skip"].ToString() |> fun s -> tryParseInt s 0
            let take = ctx.Request.Query.["take"].ToString() |> fun s -> tryParseInt s 50

            // Request one extra to determine hasMore
            let requestCount = take + 1

            let paginatedTransactions =
                state.transactions.getPageForAccount account.Name skip requestCount
                |> Seq.toArray

            let hasMore = paginatedTransactions.Length > take
            let resultTransactions =
                paginatedTransactions
                |> Seq.truncate take
                |> Seq.map Mapping.toTransactionDto
                |> Seq.toArray

            let result : PaginatedTransactionsDto = {
                Transactions = resultTransactions
                HasMore = hasMore
            }
            json result next ctx

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
                        // Parse format option
                        let formatOption =
                            if String.IsNullOrWhiteSpace(formatStr) then
                                None
                            else
                                match FormatDetector.parseFormatString formatStr with
                                | Ok f -> Some f
                                | Error _ -> None

                        // Get existing transactions for duplicate detection
                        let state = stateService.GetState()
                        let existingTransactions = state.transactions |> Seq.toList

                        // Use ImportService to parse and build preview
                        match ImportService.parseForPreview tempPath formatOption existingTransactions 0.7 with
                        | Error e ->
                            return! RequestErrors.badRequest (text e) next ctx
                        | Ok (detectedFormat, previews) ->
                            // Convert preview transactions to DTOs
                            let transactions : ParsedTransactionDto array =
                                previews
                                |> List.map (fun p ->
                                    {
                                        Date = p.Raw.Date
                                        Amount = p.Raw.Amount
                                        Description = p.Raw.Description
                                        Category = p.Category
                                        IsCredit = p.Raw.IsCredit
                                        IsDuplicate = p.IsDuplicate
                                        IsTransfer = p.IsTransfer
                                    } : ParsedTransactionDto)
                                |> List.toArray

                            let summary = ImportService.summarizePreview previews

                            let result : ParseResultDto = {
                                AccountName = accountName
                                DetectedFormat = ImportService.formatToString detectedFormat
                                Transactions = transactions
                                Summary = {
                                    Total = summary.Total
                                    Credits = summary.Credits
                                    Debits = summary.Debits
                                    Duplicates = summary.Duplicates
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

            // Map DTOs to ImportTransaction
            let importTransactions =
                importRequest.Transactions
                |> Array.map (fun dto ->
                    {
                        ImportTransaction.Date = dto.Date
                        Amount = dto.Amount
                        Description = dto.Description
                        Category = dto.Category
                        IsCredit = dto.IsCredit
                        IsTransfer = dto.IsTransfer
                        TransferAccountName = dto.TransferAccountName
                    })

            match ImportService.buildTransactions importRequest.AccountName importTransactions stateService.GetAccountByName with
            | Error errors ->
                let result : ImportResultDto = {
                    Success = false
                    Imported = 0
                    Message = String.concat "; " errors
                }
                return! RequestErrors.badRequest (json result) next ctx
            | Ok transactions ->
                match stateService.AddTransactions transactions with
                | Success () ->
                    let result : ImportResultDto = {
                        Success = true
                        Imported = transactions.Length
                        Message = sprintf "Successfully imported %d transactions" transactions.Length
                    }
                    return! json result next ctx
                | Failure msg ->
                    let result : ImportResultDto = {
                        Success = false
                        Imported = 0
                        Message = msg
                    }
                    return! ServerErrors.internalError (json result) next ctx
        }
