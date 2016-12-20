module Usemam.Ledger.Console.Parser

open System

open Usemam.Ledger.Console.Command

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

(* parser *)
let (|Prefix|_|) (p : string) (s : string) =
    match s.StartsWith p with
    | true -> p.Length |> s.Substring |> Some
    | false -> None

let matchReserved str input =
    match input with
    | Prefix str rest -> Success (str, rest)
    | _ ->
        sprintf "Expected '%s' to go first in '%s'." str input
        |> Failure

let matchSpace = matchReserved " "

let empty = String.Empty

let matchEnd (str : string) =
    match str.Trim() = empty with
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

let word (str : string) delimiter =
    let w = str |> Seq.takeWhile (fun c -> c <> delimiter) |> String.Concat
    let rest = str.Substring w.Length
    (w, rest)

let matchAmount str =
    result {
        let maybeNumber, rest = word str ' '
        let! amount =
            Amount.tryParse maybeNumber
            |> Result.fromOption (sprintf "%s is either negative or not a number." maybeNumber)
        return (amount, rest)
    }

let matchString str =
    result {
        let! _, afterQuote = matchReserved "\"" str
        let maybeString, afterString = word afterQuote '"'
        let! _, rest = matchReserved "\"" afterString
        return (maybeString, rest)
    }

let matchExit str =
    result {
        let! _, rest = matchReserved "exit" str
        let! _ = matchEnd rest
        return (Exit, empty)
    }

let matchQuery str =
    let matchAccounts str =
        result {
            let! _, rest = matchReserved "accounts" str
            let! _ = matchEnd rest
            return (Accounts, empty)
        }
    let matchToday str =
        result {
            let! _, rest = matchReserved "today" str
            let! _ = matchEnd rest
            return (Today, empty)
        }
    let matchLastWeek str =
        result {
            let! _, rest = matchReserved "last week" str
            let! _ = matchEnd rest
            return (LastWeek, empty)
        }

    result {
        let! _, rest = matchReserved "show" str
        let! _, afterSpace = matchSpace rest
        let! query, _ =
            matchAny [ matchAccounts; matchToday; matchLastWeek ] afterSpace
        return (Show query, empty)
    }

let matchAddAccount str =
    result {
        let! _, afterCommand = matchReserved "add account" str
        let! _, afterSpace1 = matchSpace afterCommand
        let! name, afterName = matchString afterSpace1
        let! _, afterSpace2 = matchSpace afterName
        let! amount, rest = matchAmount afterSpace2
        let! _ = matchEnd rest
        return (AddAccount (name, amount), empty)
    }

let matchOn str =
    let matchClock str =
        result {
            let maybeDate, rest = word str ' '
            let! date = tryCatch DateTimeOffset.Parse maybeDate
            return (Clocks.moment date, rest)
        }

    let matchFromInput input =
        result {
            let! _, afterOn = matchReserved "on" input
            let! _, afterSpace = matchSpace afterOn
            let! clock, afterClock = matchClock afterSpace
            let! _, rest = matchSpace afterClock
            return clock, rest
        }

    let now str =
        result {
            return (Clocks.machineClock, str)
        }

    result {
        let! clock, rest = matchAny [matchFromInput; now] str
        return (On clock, rest)
    }

let matchFrom str =
    result {
        let! _, afterFrom = matchReserved "from" str
        let! _, afterSpace = matchSpace afterFrom
        let! name, rest = matchString afterSpace
        return (From name, rest)
    }

let matchTo str =
    result {
        let! _, afterFrom = matchReserved "to" str
        let! _, afterSpace = matchSpace afterFrom
        let! name, rest = matchString afterSpace
        return (To name, rest)
    }

let matchTransfer str =
    result {
        let! _, afterTransfer = matchReserved "transfer" str
        let! _, afterSpace1 = matchSpace afterTransfer
        let! amount, afterAmount = matchAmount afterSpace1
        let! _, afterSpace2 = matchSpace afterAmount
        let! on, afterOn = matchOn afterSpace2
        let! from, afterFrom = matchFrom afterOn
        let! _, afterSpace3 = matchSpace afterFrom
        let! t0, afterTo = matchTo afterSpace3
        let! _ = matchEnd afterTo
        return Command.Transfer (amount, on, from, t0), empty
    }

let matchCredit str =
    result {
        let! _, afterCredit = matchReserved "credit" str
        let! _, afterSpace1 = matchSpace afterCredit
        let! amount, afterAmount = matchAmount afterSpace1
        let! _, afterSpace2 = matchSpace afterAmount
        let! on, afterOn = matchOn afterSpace2
        let! from, afterFrom = matchFrom afterOn
        let! _, afterSpace3 = matchSpace afterFrom
        let! t0, afterTo = matchTo afterSpace3
        let! _ = matchEnd afterTo
        return Command.Credit (amount, on, from, t0), empty
    }

let matchDebit str =
    result {
        let! _, afterDebit = matchReserved "debit" str
        let! _, afterSpace1 = matchSpace afterDebit
        let! amount, afterAmount = matchAmount afterSpace1
        let! _, afterSpace2 = matchSpace afterAmount
        let! on, afterOn = matchOn afterSpace2
        let! from, afterFrom = matchFrom afterOn
        let! _, afterSpace3 = matchSpace afterFrom
        let! t0, afterTo = matchTo afterSpace3
        let! _ = matchEnd afterTo
        return Command.Debit (amount, on, from, t0), empty
    }

let parse input =
    result {
        let! command, _ =
            matchAny
                [
                    matchExit
                    matchQuery
                    matchAddAccount
                    matchTransfer
                    matchCredit
                    matchDebit
                ]
                input
        return command
    }