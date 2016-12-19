namespace Usemam.Ledger.Domain

open System

module Clocks =

    let machineClock () = DateTimeOffset.Now

    let utcClock () = DateTimeOffset.UtcNow

type Period =
    | Year of int
    | Month of int * int
    | Week of int * int * int
    | Day of int * int * int

module Dates =
    
    let toOffset = DateTimeOffset

    let today clock =
        let date : DateTimeOffset = clock()
        Day(date.Year, date.Month, date.Day)

    let lastWeek (clock : unit -> DateTimeOffset) =
        let date = clock().AddDays -7.
        Week(date.Year, date.Month, date.Day)

    let BoundariesIn period =
        let getBoundaries firstTick (forward : DateTimeOffset -> DateTimeOffset) =
            let lastTick = forward(firstTick).AddTicks -1L
            (firstTick, lastTick)
        match period with
        | Year(y) -> getBoundaries (DateTime(y, 1, 1) |> toOffset) (fun d -> d.AddYears 1)
        | Month(y, m) -> getBoundaries (DateTime(y, m, 1) |> toOffset) (fun d -> d.AddMonths 1)
        | Week(y, m, d) -> getBoundaries (DateTime(y, m, d) |> toOffset) (fun d -> d.AddDays 8.)
        | Day(y, m, d) -> getBoundaries (DateTime(y, m, d) |> toOffset) (fun d -> d.AddDays 1.0)