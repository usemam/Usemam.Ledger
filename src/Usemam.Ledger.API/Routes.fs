module Usemam.Ledger.API.Routes

open Giraffe

open Usemam.Ledger.API.Handlers

let webApp : HttpHandler =
    choose [
        GET >=> choose [
            route "/api/accounts" >=> getAllAccounts
            routef "/api/accounts/%s/transactions" getTransactionsForAccount
            routef "/api/accounts/%s" getAccountByName
            routef "/api/reports/spending/%i" getSpendingReport
        ]
        RequestErrors.notFound (text "Not Found")
    ]
