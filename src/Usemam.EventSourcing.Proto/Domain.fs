namespace Usemam.EventSourcing.Proto.Domain

open System
open Usemam.EventSourcing.Proto.Infrastructure

type UserId = EventSource
type Money = double
type Category = string
type Account =
    {
        Name: string
        Balance: Money
    }

type Event =
    | AccountAdded of Account
    | TransactionCredit of Money * Category * Account
    | TransactionDebit of Money * Account * Category
    | TransactionTransfer of Money * Account * Account

type Command =
    | AddAccount of UserId * string * Money

type Total =
    {
        Amount: Money
        Category: Category
        Date: DateTimeOffset
    }

module Projections =

    let project projection =
        List.fold projection.Update projection.Init

    let private createAccount account accounts =
        accounts
        |> Map.add account.Name account
    
    let private updateAccount update target accounts =
        let account = 
            match Map.tryFind target.Name accounts with
            | Some a -> a
            | None -> target
        let updated =
            {
                Name = account.Name
                Balance = update account.Balance
            }
        accounts
        |> Map.remove account.Name
        |> Map.add account.Name updated

    let private creditAccount money =
        updateAccount (fun m -> m + money)
    
    let private debitAccount money =
        updateAccount (fun m -> m - money)
    
    let private transfer money source target =
        updateAccount (fun m -> m - money) source
        >> updateAccount (fun m -> m + money) target

    let private applyEventToAccount event =
        match event with
        | AccountAdded a -> createAccount a
        | TransactionCredit (m, _, a) -> creditAccount m a
        | TransactionDebit (m, a, _) -> debitAccount m a
        | TransactionTransfer (m, s, t) -> transfer m s t
        | _ -> id
    
    let private updateAccounts accounts event =
        accounts |> applyEventToAccount event
    
    let accountsProjection : Projection<Map<string, Account>, Event> =
        {
            Init = Map.empty
            Update = updateAccounts
        }
    
    let private updateCategory updateBalance category categories =
        let balance : Money =
            match Map.tryFind category categories with
            | Some b -> b
            | None -> 0.0
        categories
        |> Map.remove category
        |> Map.add category (updateBalance balance)
    
    let private applyEventToCategory event categories =
        match event with
        | TransactionCredit (m, c, _) -> updateCategory (fun b -> b + m) c categories
        | TransactionDebit (m, _, c) -> updateCategory (fun b -> b - m) c categories
        | _ -> categories

    let private updateCategories categories event =
        categories |> applyEventToCategory event
    
    let categoriesProjection : Projection<Map<Category, Money>, Event> =
        {
            Init = Map.empty
            Update = updateCategories
        }

module Behavior =
    let private addAccount userId name amount events =
        let account =
            {
                Name = name
                Balance = amount
            }
        AccountAdded account :: events
    let behavior command : EventProducer<Event> =
        match command with
        | AddAccount (userId, name, amount) ->
            addAccount userId name amount