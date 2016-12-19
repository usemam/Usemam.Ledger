module Usemam.Ledger.Domain.TransferProperties

open Usemam.Ledger.Domain.Tests

open System

open FsCheck
open FsCheck.Xunit

let isTransferSuccessful source dest amount =
    let initialBalance = source.Balance
    match Transaction.transferMoney source dest amount Clocks.machineClock with
    | Success t ->
        t.Sum = amount &&
            match t.Description with
            | Transfer (s, d) -> s.Balance = (initialBalance - amount) && d.Balance = amount
            | _ -> false
    | Failure _ -> false

[<Property(Arbitrary = [| typeof<MoneyArbitrary> |])>]
let ``transferMoney returns Success when amount equals to balance``
    (amount : Money) =
    let source = {
        Name = "Source"
        Created = DateTimeOffset.Now
        Balance = amount }
    let zero = Money(Amount.zero, amount.Currency)
    let dest = {
        Name = "Dest"
        Created = DateTimeOffset.Now
        Balance = zero }
    isTransferSuccessful source dest amount

[<Property(Arbitrary = [| typeof<MoneyArbitrary> |])>]
let ``transferMoney returns Success when amount less than balance``
    (amount : Money) =
    let balance = amount + Money(Amount.create 1M, amount.Currency)
    let source = {
        Name = "Source"
        Created = DateTimeOffset.Now
        Balance = balance }
    let zero = Money(Amount.zero, amount.Currency)
    let dest = {
        Name = "Dest"
        Created = DateTimeOffset.Now
        Balance = zero }
    isTransferSuccessful source dest amount

[<Property(Arbitrary = [| typeof<MoneyArbitrary> |])>]
let ``transferMoney returns Failure when amount more than balance``
    (amount : Money) =
    let balance = amount - Money(Amount.create 1M, amount.Currency)
    let source = {
        Name = "Source"
        Created = DateTimeOffset.Now
        Balance = balance }
    let zero = Money(Amount.zero, amount.Currency)
    let dest = {
        Name = "Dest"
        Created = DateTimeOffset.Now
        Balance = zero }
    isTransferSuccessful source dest amount
    |> not