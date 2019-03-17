
#r "../packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"
#r "../packages/FSharp.Data/lib/net45/FSharp.Data.dll"
#r "bin/Debug/Usemam.Ledger.Domain.dll"

#load "TransactionData.fs"

open Usemam.Ledger.Learning

let transactions = TransactionData.loadTransactions "D:\\tmp\\transactions.db"
TransactionData.transformAndSaveToCsv transactions "D:\\tmp\\data.csv"