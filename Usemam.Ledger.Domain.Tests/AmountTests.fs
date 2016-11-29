module Usemam.Ledger.Domain.AmountProperties

open FsCheck
open FsCheck.Xunit

type GreaterOrEqualToZeroDecimal =
    static member Decimal() =
        Arb.Default.Decimal () |> Arb.filter (fun x -> x >= 0M)

[<Property(Arbitrary = [| typeof<GreaterOrEqualToZeroDecimal> |])>]
let ``tryCreate should return Some when >= 0``
    (x : decimal) =
    match Amount.tryCreate x with
    | Some _ -> true
    | None -> false

type LessThanZeroDecimal =
    static member Decimal() =
        Arb.Default.Decimal () |> Arb.filter (fun x -> x < 0M)

[<Property(Arbitrary = [| typeof<LessThanZeroDecimal> |])>]
let ``tryCreate should return None when < 0``
    (x : decimal) =
    match Amount.tryCreate x with
    | Some _ -> false
    | None -> true
