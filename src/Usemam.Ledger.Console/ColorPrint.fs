module Usemam.Ledger.Console.ColorPrint

open System
open Microsoft.FSharp.Core.Printf

let cprintf color format =
    let print (s : string) = 
        let old = Console.ForegroundColor 
        try 
            Console.ForegroundColor <- color;
            Console.Write s
        finally
            Console.ForegroundColor <- old
    kprintf print format

let printEntryArrow () =
    cprintf ConsoleColor.Yellow "> "