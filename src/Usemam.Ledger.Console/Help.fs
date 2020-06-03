module Usemam.Ledger.Console.Help

open System.IO
open System.Reflection

let displayText () =
    let assembly = Assembly.GetExecutingAssembly()
    let resourceStream =
        sprintf "%s.console_commands.txt" "Usemam.Ledger.Console"
        |> assembly.GetManifestResourceStream
    use textReader = new StreamReader(resourceStream)
    textReader.ReadToEnd() |> printfn "%s"