
#r "../packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"
#r "../packages/FSharp.Data/lib/net45/FSharp.Data.dll"
#r "bin/Debug/Usemam.Ledger.Domain.dll"

#load "Data.fs"

open Usemam.Ledger.Prediction

let transactions = Data.loadTransactions "D:\\tmp\\transactions.db"
Data.transformAndSaveToCsv transactions "D:\\tmp\\data.csv"