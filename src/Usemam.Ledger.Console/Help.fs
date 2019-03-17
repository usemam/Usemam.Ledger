module Usemam.Ledger.Console.Help

open System.IO
open System.Reflection

let displayText () =
    let assembly = Assembly.GetExecutingAssembly()
    use textReader = new StreamReader(assembly.GetManifestResourceStream "console_commands.txt")
    textReader.ReadToEnd() |> printfn "%s"