module Usemam.Ledger.Console.Services

open System
open Usemam.Ledger.Console.ColorPrint

let dummy state =
    cprintfn ConsoleColor.Yellow "Doing nothing."
    state

open Usemam.Ledger.Console.Parser
open Usemam.Ledger.Domain

let fromCommand (command : Command) : Service =
    match command with
    | _ -> dummy