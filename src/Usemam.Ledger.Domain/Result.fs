namespace Usemam.Ledger.Domain

type Result<'TSuccess, 'TFailure> =
    | Success of 'TSuccess
    | Failure of 'TFailure

type Result<'T> = Result<'T, string>

type ResultBuilder() =
    member this.Bind(m, f) =
        match m with
        | Success x -> f x
        | Failure y -> Failure y
    member this.Return(x) = Success x
    member this.ReturnFrom(m) = m
    member this.Zero(m) = m

module Result =

    let tryCatch f x =
        try f x |> Success
        with | ex -> Failure ex.Message

    let fromOption failureMessage o =
        match o with
        | Some x -> Success x
        | None -> Failure failureMessage

    let result = new ResultBuilder()