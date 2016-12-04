module Usemam.Ledger.Domain.MoneyProperties

open Usemam.Ledger.Domain.Tests

open System
open Xunit
open FsCheck
open FsCheck.Xunit

[<Property(Arbitrary = [| typeof<MoneyArbitrary> |])>]
let ``+ for same currency amounts should produce correct result``
    (x : Money)
    (y : Money) =
    x.Currency = y.Currency ==> lazy
        let expected =
            x.Amount.Value + y.Amount.Value
        (x + y).Amount.Value = expected

[<Property(Arbitrary = [| typeof<MoneyArbitrary> |])>]
let ``+ for different currency amounts should produce error``
    (x : Money)
    (y : Money) =
    x.Currency <> y.Currency ==> lazy
        Assert.Throws<InvalidOperationException> (fun () -> (x + y) |> ignore)
        |> ignore

[<Property(Arbitrary = [| typeof<MoneyArbitrary> |])>]
let ``- for same currency amounts should produce correct result``
    (x : Money)
    (y : Money) =
    let expected = x.Amount.Value - y.Amount.Value
    (x.Currency = y.Currency && expected > Constants.minAmount) ==> lazy
        (x - y).Amount.Value = expected

[<Property(Arbitrary = [| typeof<MoneyArbitrary> |])>]
let ``- for different currency amounts should produce error``
    (x : Money)
    (y : Money) =
    x.Currency <> y.Currency ==> lazy
        Assert.Throws<InvalidOperationException> (fun () -> (x - y) |> ignore)
        |> ignore

[<Fact>]
let ``= for equal amounts and currencies should return true`` () =
    let amount = AmountType 10M
    let m1 = Money(amount, Currency.RUR)
    let m2 = Money(amount, Currency.RUR)
    m1 = m2