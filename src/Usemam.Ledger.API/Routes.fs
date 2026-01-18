module Usemam.Ledger.API.Routes

open Giraffe

open Usemam.Ledger.API.Handlers

let webApp : HttpHandler =
    choose [
        GET >=> choose [
            route "/api/accounts" >=> getAllAccounts
            routef "/api/accounts/%s/transactions" getTransactionsForAccount
            routef "/api/accounts/%s" getAccountByName
            route "/api/transactions" >=> getAllTransactions
        ]
        RequestErrors.notFound (text "Not Found")
    ]
