module Usemam.Ledger.Console.Parser

open System

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
    | Exit

let (|Prefix|_|) (p : string) (s : string) =
    match s.StartsWith p with
    | true -> p.Length |> s.Substring |> Some
    | false -> None

let matchString str input =
    match input with
    | Prefix str rest -> Success (str, rest)
    | _ ->
        sprintf "Expected '%s' to go first in '%s'." str input
        |> Failure

let matchSpace = matchString " "

let empty = String.Empty

let matchEndOfInput str =
    match String.IsNullOrEmpty str with
    | true -> Success(empty, str)
    | false ->
        sprintf "Expected end of input string but was '%s'." str
        |> Failure

let rec matchAny parsers str =
    match parsers with
    | [] ->
        sprintf "'%s' doesn't match with any possible option." str
        |> Failure
    | p::rest ->
        match p str with
        | Success x -> Success x
        | Failure _ -> matchAny rest str

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

let matchExit str =
    let result = new ResultBuilder()
    result {
        let! _, rest = matchString "exit" str
        let! _ = matchEndOfInput rest
        return (Exit, empty)
    }

let parse = matchExit // for testing purpose