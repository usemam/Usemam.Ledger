namespace Usemam.EventSourcing.Proto.Infrastructure

module EventStore =

    type Msg<'Event> =
        | Get of AsyncReplyChannel<EventResult<'Event>>
        | GetStream of EventSource * AsyncReplyChannel<EventResult<'Event>>
        | Append of EventEnvelope<'Event> list * AsyncReplyChannel<Result<unit,string>>
    
    let initialize (storage : EventStorage<_>) : EventStore<_> =
        let eventsAppended = Event<EventEnvelope<_> list>()

        let mailbox =
            MailboxProcessor.Start(fun inbox ->
                let rec loop () =
                    async {
                        let! msg = inbox.Receive()
                        match msg with
                        | Get reply ->
                            try
                                let! events = storage.Get()
                                do events |> reply.Reply
                            with exn ->
                                do exn.Message |> Error |> reply.Reply
                            return! loop()
                        | GetStream (source, reply) ->
                            try
                                let! events = storage.GetStream source
                                do events |> reply.Reply
                            with exn ->
                                do exn.Message |> Error |> reply.Reply
                            return! loop()
                        | Append (events, reply) ->
                            try
                                do! events |> storage.Append
                                do eventsAppended.Trigger events
                                do reply.Reply (Ok ())
                            with exn ->
                                do exn.Message |> Error |> reply.Reply
                            return! loop()
                    }
                loop()
            )
        
        {
            Get = fun () -> mailbox.PostAndAsyncReply Get
            GetStream = fun eventSource ->
                mailbox.PostAndAsyncReply (fun reply -> GetStream (eventSource,reply))
            Append = fun events ->
                mailbox.PostAndAsyncReply (fun reply -> Append (events,reply))
            OnEvents = eventsAppended.Publish
        }