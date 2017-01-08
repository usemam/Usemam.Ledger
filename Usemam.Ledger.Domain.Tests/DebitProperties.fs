module Usemam.Ledger.Domain.DebitProperties

open Usemam.Ledger.Domain.Tests

open System

open FsCheck.Xunit

let isDebitSuccessful account target amount =
    let initialBalance = account.Balance
    match Transaction.spendMoney account target amount Clocks.machineClock with
    | Success t ->
        t.Sum = amount &&
            match t.Description with
            | Debit (a, d) -> a.Balance = (initialBalance - amount) && d = target
            | _ -> false
    | Failure _ -> false

let clock = fun () -> DateTimeOffset.Now

[<Property(Arbitrary = [| typeof<MoneyArbitrary> |])>]
let ``spendMoney returns Success when amount equals to balance``
    (amount : Money) =
    let account = Account.create clock "Account" amount
    let target = DebitTarget "Target"
    isDebitSuccessful account target amount

[<Property(Arbitrary = [| typeof<MoneyArbitrary> |])>]
let ``spendMoney returns Success when amount less than balance``
    (amount : Money) =
    let account =
        amount + Money(Amount.create 1M, amount.Currency)
        |> Account.create clock "Account"
    let target = DebitTarget "Target"
    isDebitSuccessful account target amount

[<Property(Arbitrary = [| typeof<MoneyArbitrary> |])>]
let ``spendMoney returns Failure when amount more than balance``
    (amount : Money) =
    let account =
        amount - Money(Amount.create 1M, amount.Currency)
        |> Account.create clock "Account"
    let target = DebitTarget "Target"
    isDebitSuccessful account target amount
    |> not

[<Property(Arbitrary = [| typeof<MoneyArbitrary> |])>]
let ``spendMoney returns Success when amount less than balance + credit``
    (amount : Money) =
    let balance = Money(Amount.zero, amount.Currency)
    let credit = amount + Money(Amount.create 1M, amount.Currency)
    let account = Account.createWithCredit clock "Account" balance credit
    let target = DebitTarget "Target"
    isDebitSuccessful account target amount