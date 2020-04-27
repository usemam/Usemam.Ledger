namespace Usemam.EventSourcing.Proto.Infrastructure

module QueryHandler =

    let rec private choose (handlers : QueryHandler<_> list) query =
        async {
            match handlers with
            | handler :: rest -> 
                match! handler.Handle query with
                | NotHandled ->
                    return! choose rest query
                | Handled result ->
                    return Handled result
                | QueryError err ->
                    return QueryError err
            | _ -> return NotHandled
        }
    
    let initialize handlers : QueryHandler<_> =
        {
            Handle = choose handlers
        }