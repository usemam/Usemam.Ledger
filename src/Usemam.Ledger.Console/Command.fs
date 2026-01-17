module Usemam.Ledger.Console.Command

open System

open Usemam.Ledger.Domain

type From = From of string

type To = To of string

type On = On of (unit -> DateTimeOffset)

type Query =
    | Accounts
    | LastN of int * string
    | Total of DateTimeOffset * DateTimeOffset

type Command =
    | Show of Query
    | AddAccount of string * AmountType * AmountType
    | SetCreditLimit of string * AmountType
    | CloseAccount of string
    | Transfer of AmountType * On * From * To
    | Credit of AmountType * On * From * To
    | Debit of AmountType * On * From * To
    | Undo
    | Redo
    | Help
    | Exit
    | Restore
    | Backup

let isExit command =
    match command with
    | Exit -> true
    | _ -> false