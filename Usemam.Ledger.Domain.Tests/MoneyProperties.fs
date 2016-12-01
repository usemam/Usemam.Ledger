module Usemam.Ledger.Domain.MoneyProperties

open System
open Xunit
open FsCheck
open FsCheck.Xunit

type MoneyArb =
    static member Money() =
        Gen.choose(1, 1000)
        |> Gen.two
        |> Gen.map (fun (x, y) ->
            (x |> decimal |> AmountType,
             match y % 2 with
             | 1 -> Currency.RUR
             | _ -> Currency.USD))
        |> Gen.map Money
        |> Arb.fromGen

[<Property(Arbitrary = [| typeof<MoneyArb> |])>]
let ``+ for same currency amounts should produce correct result``
    (x : Money)
    (y : Money) =
    x.Currency = y.Currency ==> lazy
        let expected =
            x.Amount.Value + y.Amount.Value
        (x + y).Amount.Value = expected

[<Property(Arbitrary = [| typeof<MoneyArb> |])>]
let ``+ for different currency amounts should produce error``
    (x : Money)
    (y : Money) =
    x.Currency <> y.Currency ==> lazy
        Assert.Throws<InvalidOperationException> (fun () -> (x + y) |> ignore)
        |> ignore

[<Property(Arbitrary = [| typeof<MoneyArb> |])>]
let ``- for same currency amounts should produce correct result``
    (x : Money)
    (y : Money) =
    let expected = x.Amount.Value - y.Amount.Value
    (x.Currency = y.Currency && expected > Constants.minAmount) ==> lazy
        (x - y).Amount.Value = expected

[<Property(Arbitrary = [| typeof<MoneyArb> |])>]
let ``- for different currency amounts should produce error``
    (x : Money)
    (y : Money) =
    x.Currency <> y.Currency ==> lazy
        Assert.Throws<InvalidOperationException> (fun () -> (x - y) |> ignore)
        |> ignore