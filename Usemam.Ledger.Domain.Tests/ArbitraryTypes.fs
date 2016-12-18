namespace Usemam.Ledger.Domain.Tests

open Usemam.Ledger.Domain

open FsCheck

type MoneyArbitrary =
    static member Money() =
        Gen.choose(1, 1*1000*1000)
        |> Gen.map (fun x ->
            (x |> decimal |> Amount.create, Currency.USD))
        |> Gen.map Money
        |> Arb.fromGen
