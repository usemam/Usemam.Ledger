module Usemam.Ledger.Domain.MoneyProperties

open Usemam.Ledger.Domain.Tests

open System
open Xunit
open FsCheck
open FsCheck.Xunit

[<Property(Arbitrary = [| typeof<MoneyArbitrary> |])>]
let ``+ should produce correct result``
    (x : Money)
    (y : Money) =
    let expected =
        x.Amount.Value + y.Amount.Value
    (x + y).Amount.Value = expected

[<Property(Arbitrary = [| typeof<MoneyArbitrary> |])>]
let ``- should produce correct result``
    (x : Money)
    (y : Money) =
    let expected = x.Amount.Value - y.Amount.Value
    expected > Constants.minAmount ==> lazy
        (x - y).Amount.Value = expected

[<Fact>]
let ``= for equal amounts and currencies should return true`` () =
    let amount = Amount.create 10M
    let m1 = Money(amount, Currency.USD)
    let m2 = Money(amount, Currency.USD)
    m1 = m2