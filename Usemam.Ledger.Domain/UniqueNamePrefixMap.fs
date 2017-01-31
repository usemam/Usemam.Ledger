namespace Usemam.Ledger.Domain

open System

module UniqueNamePrefixMap =
    
    let build (names : seq<string>) =

        let getPrefixes (word : string) =
            Seq.unfold (
                fun i ->
                    if i > word.Length
                    then None
                    else Some(word.Substring(0, i), i + 1)
                ) 1

        let foldBack (prefixes : seq<seq<string>>) =
            let glue (s1 : string) (s2 : string) =
                if (String.IsNullOrEmpty s1) then s2
                else if (String.IsNullOrEmpty s2) then s2
                else s1 + " " + s2
            
            Seq.fold (fun acc e ->
                acc
                |> Seq.map (fun s ->
                    e |> Seq.map (fun p -> glue s p))
                |> Seq.concat
                ) (Seq.singleton String.Empty) prefixes

        names
        |> Seq.map (fun name ->
            let words =
                name.Split [|' '|]
                |> Seq.filter (fun w -> String.IsNullOrEmpty w |> not)
                |> Seq.toArray
            (name, words))
        |> Seq.map (fun (name, words) ->
            let prefixes =
                words |> Seq.map (fun w -> w |> getPrefixes)
            let combinations = foldBack prefixes
            combinations |> Seq.map (fun c -> (c, name)))
        |> Seq.concat
        |> Seq.groupBy (fun (c, _) -> c)
        |> Seq.filter (fun (_, g) -> Seq.length g = 1)
        |> Seq.map (fun (_, g) -> g)
        |> Seq.concat
        |> Map.ofSeq