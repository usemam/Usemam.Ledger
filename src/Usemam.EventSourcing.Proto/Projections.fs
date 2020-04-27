namespace Usemam.EventSourcing.Proto

open Domain

module Projections =

    type Projection<'State, 'Event> =
        {
            Init: 'State
            Update: 'State -> 'Event -> 'State
        }

    let project projection =
        List.fold projection.Update projection.Init
    
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

    let private applyTransactionToAccount transaction =
        match transaction with
        | Credit (m, _, a) -> creditAccount m a
        | Debit (m, a, _) -> debitAccount m a
        | Transfer (m, s, t) -> transfer m s t
    
    let private updateAccounts accounts transaction =
        accounts |> applyTransactionToAccount transaction
    
    let accountsProjection : Projection<Map<string, Account>, Transaction> =
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
    
    let private applyTransactionToCategory transaction categories =
        match transaction with
        | Credit (m, c, _) -> updateCategory (fun b -> b + m) c categories
        | Debit (m, _, c) -> updateCategory (fun b -> b - m) c categories
        | _ -> categories

    let private updateCategories categories transaction =
        categories |> applyTransactionToCategory transaction
    
    let categoriesProjection : Projection<Map<Category, Money>, Transaction> =
        {
            Init = Map.empty
            Update = updateCategories
        }