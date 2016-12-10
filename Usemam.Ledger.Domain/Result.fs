namespace Usemam.Ledger.Domain

type Result<'TSuccess, 'TFailure> =
    | Success of 'TSuccess
    | Failure of 'TFailure

module Result =

    let bind f m =
        match m with
        | Success x -> f x
        | Failure y -> Failure y

    let tryCatch f x =
        try f x |> Success
        with | ex -> Failure ex.Message

    let fromOption o =
        match o with
        | Some x -> Success x
        | None -> Failure "Provided option matches with None."

type ResultBuilder() =
    member this.Bind(m, f) = Result.bind f m
    member this.Return(x) = Success x
    member this.ReturnFrom(m) = m