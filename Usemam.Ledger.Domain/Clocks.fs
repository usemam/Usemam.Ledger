namespace Usemam.Ledger.Domain

open System

module Clocks =

    let machineClock () = DateTimeOffset.Now

    let utcClock () = DateTimeOffset.UtcNow

    let moment (date : DateTimeOffset) = fun () -> date

    let start = moment DateTimeOffset.MinValue