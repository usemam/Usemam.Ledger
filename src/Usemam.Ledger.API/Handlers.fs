module Usemam.Ledger.API.Handlers

open System
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
