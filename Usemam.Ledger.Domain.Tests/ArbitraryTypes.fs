namespace Usemam.Ledger.Domain.Tests

open Usemam.Ledger.Domain

open FsCheck

type MoneyArbitrary =
    static member Money() =
        Gen.choose(1, 1*1000*1000)
        |> Gen.two
        |> Gen.map (fun (x, y) ->
            (x |> decimal |> AmountType,
             match y % 2 with
             | 1 -> Currency.RUR
             | _ -> Currency.USD))
        |> Gen.map Money
        |> Arb.fromGen
