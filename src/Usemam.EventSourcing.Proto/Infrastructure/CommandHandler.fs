namespace Usemam.EventSourcing.Proto.Infrastructure

module CommandHandler =

    let private asEvents eventEnvelopes =
        eventEnvelopes |> List.map (fun envelope -> envelope.Event)

    let private enveloped source events =
        let now = System.DateTime.UtcNow
        let envelope event =
            {
                Metadata = {
                    Source = source
                    RecordedAtUtc = now
                }
                Event = event
            }

        events |> List.map envelope

    type Msg<'Command> =
        | Handle of EventSource * 'Command * AsyncReplyChannel<Result<unit,string>>
    
    let initialize (behavior : Behavior<_, _>) (eventStore : EventStore<_>) : CommandHandler<_> =

        let mailbox =
            MailboxProcessor.Start(fun inbox ->
                let rec loop () =
                    async {
                        let! msg = inbox.Receive()

                        match msg with
                        | Handle (source, command, reply) ->
                            let! stream = eventStore.GetStream source
                            let newEvents =
                                stream |> Result.map (asEvents >> behavior command >> enveloped source)
                            let! result =
                                match newEvents with
                                | Ok events -> eventStore.Append events
                                | Error err -> async { return Error err }
                        
                            do reply.Reply result

                            return! loop()
                    }

                loop()   
            )
        
        {
            Handle =
                fun source command ->
                    mailbox.PostAndAsyncReply (fun reply -> Handle (source, command, reply))
        }