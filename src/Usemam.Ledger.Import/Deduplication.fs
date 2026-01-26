namespace Usemam.Ledger.Import

open System
open System.Text.RegularExpressions
open Usemam.Ledger.Domain

module Deduplication =

    let private normalizeDescription (s: string) : string =
        s.ToLowerInvariant()
        |> fun x -> Regex.Replace(x, @"[#*$?&\-:]+", " ")
        |> fun x -> Regex.Replace(x, @"\d+", " ")
        |> fun x -> Regex.Replace(x, @"\s+", " ")
        |> fun x -> x.Trim()

    let private tokenize (s: string) : Set<string> =
        s.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.filter (fun t -> t.Length > 1)
        |> Set.ofArray

    let jaccardSimilarity (s1: string) (s2: string) : float =
        let tokens1 = s1 |> normalizeDescription |> tokenize
        let tokens2 = s2 |> normalizeDescription |> tokenize
        if Set.isEmpty tokens1 && Set.isEmpty tokens2 then
            1.0
        elif Set.isEmpty tokens1 || Set.isEmpty tokens2 then
            0.0
        else
            let intersection = Set.intersect tokens1 tokens2 |> Set.count |> float
            let union = Set.union tokens1 tokens2 |> Set.count |> float
            intersection / union

    type DuplicateCheckResult =
        | Unique
        | PotentialDuplicate of TransactionType * similarity: float
        | ExactDuplicate of TransactionType

    let private getTransactionDescription (t: TransactionType) : string =
        t.TextDescription
        |> Option.defaultValue (t.Description.ToString())

    let private getTransactionAmount (t: TransactionType) : decimal =
        t.Sum.Amount.Value

    let checkForDuplicate
        (existingTransactions: TransactionType seq)
        (raw: RawTransaction)
        (similarityThreshold: float)
        : DuplicateCheckResult =

        let sameDate (t: TransactionType) =
            t.Date.Date = raw.Date.Date

        let sameAmount (t: TransactionType) =
            getTransactionAmount t = raw.Amount

        let candidates =
            existingTransactions
            |> Seq.filter sameDate
            |> Seq.filter sameAmount
            |> Seq.toList

        match candidates with
        | [] -> Unique
        | matches ->
            let withSimilarity =
                matches
                |> List.map (fun t ->
                    let existingDesc = getTransactionDescription t
                    let similarity = jaccardSimilarity raw.Description existingDesc
                    (t, similarity))
                |> List.sortByDescending snd

            let (bestMatch, bestSimilarity) = withSimilarity.Head

            if bestSimilarity > 0.95 then
                ExactDuplicate bestMatch
            elif bestSimilarity >= similarityThreshold then
                PotentialDuplicate (bestMatch, bestSimilarity)
            else
                Unique

    let findDuplicates
        (existingTransactions: TransactionType seq)
        (rawTransactions: RawTransaction list)
        (similarityThreshold: float)
        : (RawTransaction * DuplicateCheckResult) list =

        rawTransactions
        |> List.map (fun raw ->
            let result = checkForDuplicate existingTransactions raw similarityThreshold
            (raw, result))

    let filterUnique
        (existingTransactions: TransactionType seq)
        (rawTransactions: RawTransaction list)
        (similarityThreshold: float)
        : RawTransaction list * RawTransaction list =

        let results = findDuplicates existingTransactions rawTransactions similarityThreshold

        let unique =
            results
            |> List.choose (fun (raw, result) ->
                match result with
                | Unique -> Some raw
                | _ -> None)

        let duplicates =
            results
            |> List.choose (fun (raw, result) ->
                match result with
                | ExactDuplicate _ | PotentialDuplicate _ -> Some raw
                | _ -> None)

        (unique, duplicates)
