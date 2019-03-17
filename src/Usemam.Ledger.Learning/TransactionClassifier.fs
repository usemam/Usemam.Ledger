namespace Usemam.Ledger.Learning

open System

open Usemam.Ledger.Learning.NaiveBayes

module TransactionClassifier =

    let descriptionTokenizer (text : string) =
        let isNumeric (s : string) =
            s.ToCharArray()
            |> Array.filter Char.IsDigit
            |> Array.length
            |> (<) 0
        let isSymbol s =
            ["#"; "$"; "?"; "&";"-";":"]
            |> Seq.tryFind (fun x -> x = s)
            |> Option.isSome
        text.ToLowerInvariant().Split(' ')
        |> Array.filter (isNumeric >> not)
        |> Array.filter (isSymbol >> not)
        |> Array.filter (fun s -> s.Length > 1)
        |> Set.ofArray
    
    let trainClassifier (data : (string * string) []) =
        let allTokens =
            data
            |> Seq.map snd
            |> Classifier.vocabulary descriptionTokenizer
        let model = Classifier.learn data descriptionTokenizer allTokens
        let classifier = Classifier.classify model descriptionTokenizer
        (classifier, model)