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

let matchCount str =
    result {
        let maybeNumber, rest = word str ' '
        let! count = Result.tryCatch Int32.Parse maybeNumber
        return (count, rest)
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
    let matchLastN str =
        result {
            let! _, beforeN = all [reserved "last"; space] str
            let! n, rest = matchCount beforeN
            let! _ = fin rest
            return LastN n
        }
    let total str =
        result {
            let matchTotal str =
                result {
                    let! _ = all [reserved "total"; fin] str
                    return (Clocks.start(), Clocks.machineClock())
                }
            let matchTotalWithDates str =
                let date str =
                    result {
                        let maybeDate, rest = word str ' '
                        let! date = tryCatch DateTimeOffset.Parse maybeDate
                        return (date, rest)
                    }
                result {
                    let! _, beforeMin = all [reserved "total"; space] str
                    let! min, afterMin = date beforeMin
                    let! _, beforeMax = all [space; reserved "to"; space] afterMin
                    let! max, afterMax = date beforeMax
                    let! _ = fin afterMax
                    return (min, max)
                }
            let! timeFrame = any [matchTotal; matchTotalWithDates] str
            return Total timeFrame
        }
    result {
        let! _, rest = all [reserved "show"; space] str
        let! query =
            any [ matchAccounts; matchLastN; total ] rest
        return Show query
    }

let someCredit s =
    result {
        let! _, maybeAmount = all [space; reserved "credit"; space] s
        let! amount, rest = amount maybeAmount
        let! _ = fin rest
        return amount
    }

let addAccount str =
    let noCredit s =
        result {
            let! _ = fin s
            return Amount.zero
        }
    result {
        let! _, maybeName = all [reserved "add account"; space] str
        let! name, afterName = matchString maybeName
        let! _, maybeAmount = space afterName
        let! amount, rest = amount maybeAmount
        let! credit = any [ noCredit; someCredit ] rest
        return AddAccount (name, amount, credit)
    }

let setCreditLimit str =
    result {
        let! _, maybeName = all [reserved "set account"; space] str
        let! name, afterName = matchString maybeName
        let! credit = someCredit afterName
        return SetCreditLimit(name, credit)
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
        let! _ = all [reserved "help"; fin] str
        return Help
    }

let undo str =
    result {
        let! _ = all [reserved "undo"; fin] str
        return Undo
    }

let redo str =
    result {
        let! _ = all [reserved "redo"; fin] str
        return Redo
    }

let parse  =
    any [ exit; help; show; addAccount; setCreditLimit; transfer; credit; debit; undo; redo; ]