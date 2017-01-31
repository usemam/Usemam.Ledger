module Usemam.Ledger.Domain.UniqueNamePrefixMapTests

open Xunit

[<Fact>]
let ``should return all prefixes including name itself`` () =
    let name = "test"
    let map = UniqueNamePrefixMap.build [name]
    ["t";"te";"tes";name] =
        (map |> Map.toSeq |> Seq.map (fun (_, s) -> s) |> Seq.toList)

[<Fact>]
let ``should return all correct prefixes for 2-word name`` () =
    let name = "acc 1"
    let map = UniqueNamePrefixMap.build [name]
    ["a 1";"ac 1";name] =
        (map |> Map.toSeq |> Seq.map (fun (_, s) -> s) |> Seq.toList)

[<Fact>]
let ``should return all unique prefixes for 2+ names`` () =
    let names = ["wf check"; "wf credit"]
    let map = UniqueNamePrefixMap.build names
    map
    |> Map.toSeq
    |> Seq.map (fun (_, s) -> s)
    |> Seq.filter (fun s -> s = "w c" || s = "wf c")
    |> Seq.length = 0