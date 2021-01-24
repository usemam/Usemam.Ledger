#r "nuget: FSharp.Data"
open FSharp.Data

[<Literal>]
let path = "learning_data_full_path.csv"

type LearningData = CsvProvider<path>

let dataset =
    LearningData.Load path
    |> (fun doc ->
            doc.Rows
            |> Seq.map (fun r -> (r.Category, r.Description)))
    |> Array.ofSeq

#load "NaiveBayes.fs"
#load "TransactionClassifier.fs"
open Usemam.Ledger.Learning

let validation, training = dataset, dataset

let evaluate () =
    let classifier, _ = TransactionClassifier.trainClassifier training
    validation
    |> Seq.averageBy (fun (exp, desc) ->
        let actual = classifier desc
        //printfn "Transaction '%s' classified as '%O'" desc actual
        if exp = actual then 1. else 0.)
    |> printfn "Correctly classified: %.3f"

evaluate()