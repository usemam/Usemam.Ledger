open System.IO

#r "../packages/FSharp.Data/lib/net45/FSharp.Data.dll"
open FSharp.Data
open System

[<Literal>]
let path = "D:\\tmp\\class_learning.csv"

type LearningData = CsvProvider<path>

type TransactionClass =
    | Apparel
    | Car
    | Debt
    | Entertainment
    | FeesAndTaxes
    | Gas
    | Grocery
    | Health
    | Home
    | Interest
    | Kid
    | Lunch
    | Misc
    | Rent
    | Returns
    | Salary
    | Utilities
    | Transfer

let parseClass label =
    match label with
    | "Apparel" -> Apparel
    | "Car" -> Car
    | "Debt" -> Debt
    | "Entertainment" -> Entertainment
    | "Fees & Taxes" -> FeesAndTaxes
    | "Gas" -> Gas
    | "Grocery" -> Grocery
    | "Health" -> Health
    | "Home" -> Home
    | "Interest" -> Interest
    | "Kid" -> Kid
    | "Lunch" -> Lunch
    | "Misc" -> Misc
    | "Rent" -> Rent
    | "Returns" -> Returns
    | "Salary" -> Salary
    | "Utilities" -> Utilities
    | "Transfer" -> Transfer
    | c -> failwithf "Unsupported transaction class '%s'" c

let dataset =
    LearningData.Load path
    |> (fun doc -> doc.Rows
                   |> Seq.map (fun r -> (parseClass r.Category, r.Description)))
    |> Array.ofSeq

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

#load "NaiveBayes.fs"
open Usemam.Ledger.Learning.NaiveBayes

let validation, training = dataset.[500..], dataset.[0..499]

let evaluate (tokenizer : Tokenizer) (tokens : Token Set) =
    let model = Classifier.train training tokenizer tokens
    validation
    |> Seq.averageBy (fun (exp, desc) ->
        let actual = model desc
        printfn "Transaction '%s' classified as '%O'" desc actual
        if exp = actual then 1. else 0.)
    |> printfn "Correctly classified: %.3f"

let allTokens =
    training
    |> Seq.map snd
    |> Classifier.vocabulary descriptionTokenizer

printfn "All tokens count is %i" allTokens.Count
//allTokens |> Set.map (printfn "%s")

evaluate descriptionTokenizer allTokens