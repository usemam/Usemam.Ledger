namespace Usemam.EventSourcing.Proto.Infrastructure

module EventListener =

    type Msg<'Event> =
        | Notify of EventEnvelope<'Event> list
        | Subscribe of EventHandler<'Event>
    
    let notifyEventHandlers events (handlers : EventHandler<_> list) =
        handlers
        |> List.map (fun handler -> handler events)
        |> Async.Parallel
        |> Async.Ignore
    
    let initialize () =
        let mailbox =
            MailboxProcessor.Start(fun inbox ->
                let rec loop handlers =
                    async {
                        let! msg = inbox.Receive()

                        match msg with
                        | Notify events ->
                            do! notifyEventHandlers events handlers
                            return! loop handlers
                        | Subscribe handler ->
                            return! loop (handler :: handlers)
                    }

                loop []
            )
        
        {
            Notify = Notify >> mailbox.Post
            Subscribe = Subscribe >> mailbox.Post
        }