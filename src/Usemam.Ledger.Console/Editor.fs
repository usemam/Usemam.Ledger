module Usemam.Ledger.Console.Editor

open System.Collections.Generic
open System.Linq
open Usemam.Ledger.CommandLine
open Usemam.Ledger.Console.Keywords
open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result
open Usemam.Ledger.Domain.Queries

let createEditor (trackerResult : Result<CommandTracker, string>) =
    result {
        let! tracker = trackerResult
        let accountsQuery = GetAllAccountsQuery() :> IQuery<seq<AccountType>>
        let! accounts = accountsQuery.run tracker.state
        let categoriesQuery = GetAllCategoriesQuery() :> IQuery<seq<string>>
        let! categories = categoriesQuery.run tracker.state
        let autocomplete = List.concat [
            AllKeywords
            accounts |> Seq.map _.Name |> Seq.toList
            categories |> Seq.toList
        ]
        let history = List<string>()
        return InputEditor(history, autocomplete.ToList())
    }