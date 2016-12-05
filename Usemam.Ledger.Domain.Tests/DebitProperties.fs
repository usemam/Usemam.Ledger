module Usemam.Ledger.Domain.DebitProperties

open Usemam.Ledger.Domain.Tests

open System

open FsCheck.Xunit

let isDebitSuccessful account target amount =
    let initialBalance = account.Balance
    match Debit.spendMoney account target amount with
    | Success t ->
        t.Sum = amount &&
            match t.Description with
            | Debit (a, d) -> a.Balance = (initialBalance - amount) && d = target
            | _ -> false
    | Failure _ -> false

[<Property(Arbitrary = [| typeof<MoneyArbitrary> |])>]
let ``spendMoney returns Success when amount equals to balance``
    (amount : Money) =
    let account =
        {
            Name = "Account"
            Created = DateTimeOffset.Now
            Balance = amount
        }
    let target = DebitTarget "Target"
    isDebitSuccessful account target amount

[<Property(Arbitrary = [| typeof<MoneyArbitrary> |])>]
let ``spendMoney returns Success when amount less than balance``
    (amount : Money) =
    let account =
        {
            Name = "Account"
            Created = DateTimeOffset.Now
            Balance = amount + Money(Amount.create 1M, amount.Currency)
        }
    let target = DebitTarget "Target"
    isDebitSuccessful account target amount

[<Property(Arbitrary = [| typeof<MoneyArbitrary> |])>]
let ``spendMoney returns Failure when amount more than balance``
    (amount : Money) =
    let account =
        {
            Name = "Account"
            Created = DateTimeOffset.Now
            Balance = amount - Money(Amount.create 1M, amount.Currency)
        }
    let target = DebitTarget "Target"
    isDebitSuccessful account target amount
    |> not