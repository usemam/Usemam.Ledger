module Usemam.Ledger.Prediction.Data

open System.IO

open Newtonsoft.Json

open FSharp.Data

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

let private loadJson fileName = File.ReadAllText fileName

let private deserialize<'T> json = JsonConvert.DeserializeObject<'T> json

let loadTransactions transactionsFile =
    result {
        let! transactionsJson = tryCatch loadJson transactionsFile
        return! tryCatch deserialize<TransactionType list> transactionsJson
    }

type CsvType =
    CsvProvider<
        Schema = "Year (int), Month (int), DayOfMonth (int), DayOfWeek (int), Amount (decimal), Category (string)",
        HasHeaders=false>

let transformAndSaveToCsv (transactionsResult : Result<TransactionType list, string>) (filePath : string) =
    let getCategory (transactionDesc : TransactionDescription) =
        match transactionDesc with
        | Credit (_, source) -> source.ToString()
        | Debit (_, target) -> target.ToString()
        | Transfer _ -> "Transfer"
    let getAmount (transaction : TransactionType) =
        match transaction.Description with
        | Credit _ -> transaction.Sum.Amount.Value
        | Debit _ -> -transaction.Sum.Amount.Value
        | Transfer _ -> failwith "Transfer transactions are not allowed"
    result {
        let! transactions = transactionsResult
        
        let creditTransactions =
            transactions
            |> Seq.filter (fun t -> match t.Description with | Credit _ -> true | _ -> false)
        printfn "%i total credit transactions" (creditTransactions |> Seq.length)

        let debitTransactions =
            transactions
            |> Seq.filter (fun t -> match t.Description with | Debit _ -> true | _ -> false)
        printfn "%i total debit transactions" (debitTransactions |> Seq.length)

        let rows =
            creditTransactions
            |> Seq.append debitTransactions
            |> Seq.sortBy (fun t -> t.Date)
            |> Seq.map (fun t -> CsvType.Row(t.Date.Year, t.Date.Month, t.Date.Day, int t.Date.DayOfWeek, getAmount t, getCategory t.Description))
        let csv = new CsvType(rows)
        return csv.Save filePath
    }