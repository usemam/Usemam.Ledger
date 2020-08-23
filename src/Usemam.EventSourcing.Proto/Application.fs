namespace Usemam.EventSourcing.Proto

module API =

    open System
    open Usemam.EventSourcing.Proto.Domain

    type Query =
        | GetAccounts of UserId
        | GetLastTransactions of UserId * int * string option
        | GetTotal of UserId * DateTimeOffset option * DateTimeOffset option

module QueryHandlers =

    open System
    open API
    open Usemam.EventSourcing.Proto.Domain
    open Usemam.EventSourcing.Proto.Infrastructure

    let getAccounts accounts : QueryHandler<Query> =
        let handleQuery query =
            match query with
            | GetAccounts(userId) ->
                async {
                    let! state = accounts userId

                    return state
                        |> Option.defaultValue Map.empty
                        |> box
                        |> Handled
                }

            | _ -> async { return NotHandled }
        
        { Handle = handleQuery }
    
    let getLastTransactions events : QueryHandler<Query> =
        let filterEventsByAccount (eventList : Event list) accountName =
            let matchName account =
                account.Name.StartsWith(accountName, StringComparison.OrdinalIgnoreCase)
            eventList
            |> List.filter (fun t ->
                match t with
                | TransactionCredit(_, _, a) -> matchName a
                | TransactionDebit (_, a, _) -> matchName a
                | TransactionTransfer(_, a1, a2) -> matchName a1 || matchName a2
                | _ -> false)
        let handleQuery query =
            match query with
            | GetLastTransactions(userId, n, maybeAccountName) ->
                async {
                    let! state = events userId

                    return state
                        |> Option.defaultValue List.empty
                        |> (fun eventList ->
                            match maybeAccountName with
                            | None -> eventList
                            | Some accountName -> filterEventsByAccount eventList accountName)
                        |> List.take n
                        |> box
                        |> Handled
                }

            | _ -> async { return NotHandled }
        
        { Handle = handleQuery }
    
    let getTotal totals : QueryHandler<Query> =
        let filterTotalsByStartDate (totalList : Total list) startDate =
            totalList
            |> List.filter (fun t -> t.Date > startDate)
        let filterTotalsByEndDate (totalList : Total list) endDate =
            totalList
            |> List.filter (fun t -> t.Date < endDate)
        let addOrUpdate (map : Map<string, Money>) (total : Total) =
            let (map', amount) =
                match map.TryFind total.Category with
                | None -> (map, total.Amount)
                | Some a -> (map.Remove total.Category, a + total.Amount)
            map'.Add (total.Category, amount)
        let handleQuery query =
            match query with
            | GetTotal(userId, maybeStartDate, maybeEndDate) ->
                async {
                    let! state = totals userId

                    return state
                        |> Option.defaultValue List.empty
                        |> (fun totalList ->
                            match maybeStartDate with
                            | None -> totalList
                            | Some startDate -> filterTotalsByStartDate totalList startDate)
                        |> (fun totalList ->
                            match maybeEndDate with
                            | None -> totalList
                            | Some endDate -> filterTotalsByEndDate totalList endDate)
                        |> List.fold addOrUpdate Map.empty
                        |> box
                        |> Handled
                }

            | _ -> async { return NotHandled }
        { Handle = handleQuery }