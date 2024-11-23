module Usemam.Ledger.Console.Input

type KeyPress =
    | ArrowLeft
    | ArrowRight
    | ArrowUp
    | ArrowDown

type Input =
    | Key of KeyPress
    | String of string

let readInput () =
    let newLine = System.Console.In.ReadLine()
    String newLine

let moveCaret (delta : int) =
    ()