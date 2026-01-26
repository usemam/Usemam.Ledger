namespace Usemam.Ledger.Import

open System
open Usemam.Ledger.Domain

module Classification =

    let private extractCategory (t: TransactionType) : string option =
        match t.Description with
        | Credit (_, CreditSource source) -> Some source
        | Debit (_, DebitTarget target) -> Some target
        | Transfer _ -> None

    let getExistingCategories (existingTransactions: TransactionType seq) : string list =
        existingTransactions
        |> Seq.choose extractCategory
        |> Seq.distinct
        |> Seq.toList

    /// Classify transactions using CSV-provided category or default
    let classifyWithFallback
        (defaultCategory: string)
        (rawTransactions: RawTransaction list)
        : (RawTransaction * string) list =

        rawTransactions
        |> List.map (fun raw ->
            let category =
                match raw.Category with
                | Some cat when not (String.IsNullOrWhiteSpace(cat)) -> cat
                | _ -> defaultCategory
            (raw, category))

    /// Simple classification - uses CSV category or default
    let classifyTransactions
        (defaultCategory: string)
        (rawTransactions: RawTransaction list)
        : (RawTransaction * string) list =

        classifyWithFallback defaultCategory rawTransactions
