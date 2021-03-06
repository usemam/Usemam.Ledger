﻿module Usemam.Ledger.Domain.AmountProperties

open System

open FsCheck
open FsCheck.Xunit

type ValidDecimal =
    static member Decimal() =
        Arb.Default.Decimal()
        |> Arb.filter (fun x -> Math.Abs(x) >= Constants.minAmount && Math.Abs(x) <= 1000000M)

[<Property(Arbitrary = [| typeof<ValidDecimal> |])>]
let ``tryCreate should return Some when input is valid``
    (x : decimal) =
    match Amount.tryCreate x with
    | Some y -> x = y.Value
    | None -> false

type InvalidDecimal =
    static member Decimal() =
        Arb.Default.Decimal() 
        |> Arb.filter (fun x -> Math.Abs(x) < Constants.minAmount)

[<Property(Arbitrary = [| typeof<InvalidDecimal> |])>]
let ``tryCreate should return None when input is invalid``
    (x : decimal) =
    match Amount.tryCreate x with
    | Some _ -> false
    | None -> true

type NumericString =
    // generates string which is a number between 1 and 1000
    static member String() =
        Gen.choose(1, 1000) |> Gen.map string |> Arb.fromGen

[<Property(Arbitrary = [| typeof<NumericString> |])>]
let ``tryParse should return Some when string is a number``
    (s : string) =
    match Amount.tryParse s with
    | Some _ -> true
    | None -> false

type NonNumericString =
    static member String() =
        Arb.Default.String() |> Arb.filter (fun s -> s |> (not << fst << System.Decimal.TryParse))

[<Property(Arbitrary = [| typeof<NonNumericString> |])>]
let ``tryParse should return None when string is not a number``
    (s : string) =
    match Amount.tryParse s with
    | None -> true
    | Some _ -> false

[<Property(Arbitrary = [| typeof<ValidDecimal> |])>]
let ``+ should produce sum of two values``
    (x : decimal)
    (y : decimal) =
    let a = Amount.create x
    let b = Amount.create y
    (a + b).Value = x + y

[<Property(Arbitrary = [| typeof<ValidDecimal> |])>]
let ``- should produce correct result when first operand greater than second``
    (x : decimal)
    (y : decimal) =
    x - y >= Constants.minAmount ==> lazy
        let a = Amount.create x
        let b = Amount.create y
        (a - b).Value = x - y

[<Property(Arbitrary = [| typeof<ValidDecimal> |])>]
let ``- should produce correct result when first operand less than second``
    (x : decimal)
    (y : decimal) =
    (x < y && Math.Abs(x - y) >= Constants.minAmount) ==> lazy
        let a = Amount.create x
        let b = Amount.create y
        (a - b).Value = x - y