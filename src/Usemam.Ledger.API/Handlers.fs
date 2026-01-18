module Usemam.Ledger.API.Handlers

open System
open Microsoft.AspNetCore.Http
open Giraffe

open Usemam.Ledger.Domain
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

let getAllTransactions : HttpHandler =
    fun next ctx ->
        let stateService = getStateService ctx
        let state = stateService.GetState()

        let startParam = ctx.TryGetQueryStringValue "start"
        let endParam = ctx.TryGetQueryStringValue "end"

        let transactions =
            match startParam, endParam with
            | Some startStr, Some endStr ->
                match DateTimeOffset.TryParse(startStr), DateTimeOffset.TryParse(endStr) with
                | (true, startDate), (true, endDate) ->
                    state.transactions.between startDate endDate
                | _ -> state.transactions :> seq<_>
            | _ -> state.transactions :> seq<_>

        let transactionDtos =
            transactions
            |> Seq.map Mapping.toTransactionDto
            |> Seq.toList

        json transactionDtos next ctx
