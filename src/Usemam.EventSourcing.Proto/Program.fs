module Domain =

    type Money = double
    type Category = string
    type Account =
        {
            Name: string
            Balance: Money
        }

    type Transaction =
        | Credit of Money * Category * Account
        | Debit of Money * Account * Category
        | Transfer of Money * Account * Account

module EventStore =

    type public EventStore<'Event> =
        {
            Get: unit -> 'Event list
            Append: 'Event list -> unit
        }
    
    type public Projection<'State, 'Event> =
        {
            Init: 'State
            Update: 'State -> 'Event -> 'State
        }
    
    type private Msg<'Event> =
        | Get of AsyncReplyChannel<'Event list>
        | Append of 'Event list
    
    let initialize () : EventStore<'Event> =
        
        let history = []

        let mailbox =
            MailboxProcessor.Start(fun inbox ->
                let rec loop history =
                  async {
                    let! msg = inbox.Receive()

                    match msg with
                    | Get reply ->
                        reply.Reply history
                        return! loop history

                    | Append events  ->
                        return! loop (history @ events)
                  }

                loop history
            )

        let get () =
            mailbox.PostAndReply Get
        
        let append events =
            events
            |> Append
            |> mailbox.Post
        
        {
            Get = get
            Append = append
        }

open System
open Domain
open EventStore

module Helper =

    let printEvents events =
        events
        |> List.map (fun e ->
            match e with
            | Credit (m, c, a) -> printfn "Credited %f from %s to %s" m c a.Name
            | Debit (m, a, c) -> printfn "Debited %f from %s to %s" m a.Name c
            | Transfer (m, a1, a2) -> printfn "Transferred %f from %s to %s" m a1.Name a2.Name
        )

[<EntryPoint>]
let main argv =
    let eventStore : EventStore<Transaction> = initialize()
    let account =
        {
            Name = "Cash"
            Balance = 0.0
        }
    eventStore.Append [Credit (100.0, "Income", account)]
    eventStore.Append [Debit (15.0, account, "Grocery")]
    eventStore.Append [Debit (7.5, account, "Entertainment")]

    eventStore.Get ()
    |> Helper.printEvents
    |> ignore
    0 // return an integer exit code
