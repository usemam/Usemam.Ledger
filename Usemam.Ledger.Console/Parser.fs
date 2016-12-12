module Usemam.Ledger.Console.Parser

open Usemam.Ledger.Domain

type Result<'T> = Result<'T, string>

type Parser<'T> = Parser of (string -> Result<'T * string>)

type From = From of string

type To = To of string

type Query =
    | Accounts

type Command =
    | Show of Query
    | AddAccount of string * AmountType
    | Transfer of AmountType * From * To
    | Credit of AmountType * From * To
    | Debit of AmountType * From * To

let (|Prefix|_|) (p : string) (s : string) =
    match s.StartsWith p with
    | true -> p.Length |> s.Substring |> Some
    | false -> None

let matchSpace str =
    match str with
    | Prefix " " rest -> Success (" ", rest)
    | _ ->
        sprintf "Expected ' ' to go first in '%s'." str
        |> Failure

open System

let word (str : string) =
    let w = str |> Seq.takeWhile (fun c -> c <> ' ') |> String.Concat
    let rest = str.Substring w.Length
    (w, rest)

let matchAmount str =
    let result = new ResultBuilder()
    result {
        let! _, afterSpace = matchSpace str
        let maybeNumber, rest = word afterSpace
        let! amount =
            Amount.tryParse maybeNumber
            |> Result.fromOption (sprintf "%s is either negative or not a number." maybeNumber)
        return (amount, rest)
    }