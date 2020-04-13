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

    let private credit money =
        updateAccount (fun m -> m + money)
    
    let private debit money =
        updateAccount (fun m -> m - money)
    
    let private transfer money source target accounts =
        accounts
        |> updateAccount (fun m -> m - money) source
        |> updateAccount (fun m -> m + money) target

    let private mapTransaction transaction =
        match transaction with
        | Credit (m, c, a) -> credit m a
        | Debit (m, a, c) -> debit m a
        | Transfer (m, s, t) -> transfer m s t
    
    let private updateAccounts accounts transaction =
        accounts |> mapTransaction transaction        
    
    let accountsProjection : Projection<Map<string, Account>, Transaction> =
        {
            Init = Map.empty
            Update =  updateAccounts
        }