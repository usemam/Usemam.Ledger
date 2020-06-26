namespace Usemam.EventSourcing.Proto

open System

module Domain =

    type Money = double
    type Category = string
    type Account =
        {
            Name: string
            Balance: Money
        }

    type Transaction =
        | Credit of Money * Category * Account
        | Debit of Money * Account * Category
        | Transfer of Money * Account * Account
    
    type Total =
        {
            Amount: Money
            Category: Category
            Date: DateTimeOffset
        }