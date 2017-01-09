module Usemam.Ledger.Console.Parser

open System

open Usemam.Ledger.Console.Command

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

let (|Prefix|_|) (p : string) (s : string) =
    match s.StartsWith p with
    | true -> p.Length |> s.Substring |> Some
    | false -> None

let reserved str input =
    match input with
    | Prefix str rest -> Success (str, rest)
    | _ ->
        sprintf "Expected '%s' to go first in '%s'." str input
        |> Failure

let space = reserved " "

let empty = String.Empty

let fin (str : string) =
    match str.Trim() = empty with
    | true -> Success(empty, str)
    | false ->
        sprintf "Expected end of input string but was '%s'." str
        |> Failure

let rec any parsers str =
    match parsers with
    | [] ->
        sprintf "'%s' doesn't match with any possible option." str
        |> Failure
    | p::rest ->
        match p str with
        | Success x -> Success x
        | Failure _ -> any rest str

let all parsers str =
    let rec innerMatch parsers parsed str =
        result {
            match parsers with
            | [] -> return (parsed, str)
            | p::ps ->
                let! x, rest = p str
                return! innerMatch ps (x::parsed) rest
        }
    result {
        let! matched, rest = innerMatch parsers [] str
        return matched |> List.rev, rest
    }

let word (str : string) delimiter =
    let w = str |> Seq.takeWhile (fun c -> c <> delimiter) |> String.Concat
    let rest = str.Substring w.Length
    (w, rest)

let amount str =
    result {
        let maybeNumber, rest = word str ' '
        let! amount =
            Amount.tryParse maybeNumber
            |> Result.fromOption (sprintf "%s is either negative or not a number." maybeNumber)
        return (amount, rest)
    }

let matchString str =
    result {
        let! _, afterQuote = reserved "\"" str
        let maybeString, afterString = word afterQuote '"'
        let! _, rest = reserved "\"" afterString
        return (maybeString, rest)
    }

let exit str =
    result {
        let! _ = all [reserved "exit"; fin] str
        return Exit
    }

let show str =
    let matchAccounts str =
        result {
            let! _ = all [reserved "accounts"; fin] str
            return Accounts
        }
    let matchToday str =
        result {
            let! _ = all [reserved "today"; fin] str
            return Today
        }
    let matchLastWeek str =
        result {
            let! _ = all [reserved "last week"; fin] str
            return LastWeek
        }
    result {
        let! _, rest = all [reserved "show"; space] str
        let! query =
            any [ matchAccounts; matchToday; matchLastWeek ] rest
        return Show query
    }

let addAccount str =
    let noCredit s =
        result {
            let! _ = fin s
            return Amount.zero
        }
    let someCredit s =
        result {
            let! _, maybeAmount = all [space; reserved "credit"; space] s
            let! amount, rest = amount maybeAmount
            let! _ = fin rest
            return amount
        }
    result {
        let! _, afterCommand = reserved "add account" str
        let! _, afterSpace1 = space afterCommand
        let! name, afterName = matchString afterSpace1
        let! _, afterSpace2 = space afterName
        let! amount, rest = amount afterSpace2
        let! credit = any [ noCredit; someCredit ] rest
        return AddAccount (name, amount, credit)
    }

let details str =

    let matchOn str =
        let clock str =
            result {
                let maybeDate, rest = word str ' '
                let! date = tryCatch DateTimeOffset.Parse maybeDate
                return (Clocks.moment date, rest)
            }
        let fromInput input =
            result {
                let! _, afterOn = reserved "on" input
                let! _, afterSpace = space afterOn
                let! clock, afterClock = clock afterSpace
                let! _, rest = space afterClock
                return clock, rest
            }
        let now str =
            result {
                return (Clocks.machineClock, str)
            }
        result {
            let! clock, rest = any [fromInput; now] str
            return (On clock, rest)
        }

    let matchFrom str =
        result {
            let! _, afterFrom = reserved "from" str
            let! _, afterSpace = space afterFrom
            let! name, rest = matchString afterSpace
            return (From name, rest)
        }

    let matchTo str =
        result {
            let! _, afterFrom = reserved "to" str
            let! _, afterSpace = space afterFrom
            let! name, rest = matchString afterSpace
            return (To name, rest)
        }

    result {
        let! _, afterSpace1 = space str
        let! amount, afterAmount = amount afterSpace1
        let! _, afterSpace2 = space afterAmount
        let! on, afterOn = matchOn afterSpace2
        let! from, afterFrom = matchFrom afterOn
        let! _, afterSpace3 = space afterFrom
        let! t0, afterTo = matchTo afterSpace3
        let! _ = fin afterTo
        return (amount, on, from, t0)
    }

let transfer str =
    result {
        let! _, afterTransfer = reserved "transfer" str
        let! amount, on, from, t0 = details afterTransfer
        return Command.Transfer (amount, on, from, t0)
    }

let credit str =
    result {
        let! _, afterCredit = reserved "credit" str
        let! amount, on, from, t0 = details afterCredit
        return Command.Credit (amount, on, from, t0)
    }

let debit str =
    result {
        let! _, afterDebit = reserved "debit" str
        let! amount, on, from, t0 = details afterDebit
        return Command.Debit (amount, on, from, t0)
    }

let help str =
    result {
        let! _, rest = reserved "help" str
        let! _ = fin rest
        return Help
    }

let parse  =
    any [ exit; help; show; addAccount; transfer; credit; debit ]