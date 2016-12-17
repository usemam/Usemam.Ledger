namespace Usemam.Ledger.Domain

open System

module Clocks =

    let machineClock () = DateTimeOffset.Now

    let utcClock () = DateTimeOffset.UtcNow

type Period =
    | Year of int
    | Month of int * int
    | Day of int * int * int

module Dates =
    
    let toOffset = DateTimeOffset

    let InitInfinite (date : DateTimeOffset) =
        date |> Seq.unfold (fun d -> Some(d, d.AddDays 1.0))

    let In period =
        let generate dt predicate =
            dt |> InitInfinite |> Seq.takeWhile predicate
        match period with
        | Year(y) -> generate (DateTime(y, 1, 1) |> toOffset) (fun d -> d.Year = y)
        | Month(y, m) -> generate (DateTime(y, m, 1) |> toOffset) (fun d -> d.Month = m)
        | Day(y, m, d) -> DateTime(y, m, d) |> toOffset |> Seq.singleton

    let BoundariesIn period =
        let getBoundaries firstTick (forward : DateTimeOffset -> DateTimeOffset) =
            let lastTick = forward(firstTick).AddTicks -1L
            (firstTick, lastTick)
        match period with
        | Year(y) -> getBoundaries (DateTime(y, 1, 1) |> toOffset) (fun d -> d.AddYears 1)
        | Month(y, m) -> getBoundaries (DateTime(y, m, 1) |> toOffset) (fun d -> d.AddMonths 1)
        | Day(y, m, d) -> getBoundaries (DateTime(y, m, d) |> toOffset) (fun d -> d.AddDays 1.0)