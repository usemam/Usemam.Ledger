namespace Usemam.EventSourcing.Proto

module EventStore =

    type Aggregate = System.Guid

    type EventStore<'Event> =
        {
            Get: unit -> Map<Aggregate, 'Event list>
            GetStream: Aggregate -> 'Event list
            Append: Aggregate -> 'Event list -> unit
        }
    
    type private Msg<'Event> =
        | Get of AsyncReplyChannel<Map<Aggregate, 'Event list>>
        | GetStream of Aggregate * AsyncReplyChannel<'Event list>
        | Append of Aggregate * 'Event list
    
    let initialize () : EventStore<'Event> =

        let getAggregateStream aggregate history =
            history
            |> Map.tryFind aggregate
            |> Option.defaultValue []

        let mailbox =
            MailboxProcessor.Start(fun inbox ->
                let rec loop history =
                  async {
                    let! msg = inbox.Receive()

                    match msg with
                    | Get reply ->
                        reply.Reply history
                        return! loop history
                    
                    | GetStream (aggregate, reply) ->
                        let stream =
                            getAggregateStream aggregate history
                        reply.Reply stream
                        return! loop history

                    | Append (aggregate, events)  ->
                        let stream = getAggregateStream aggregate history
                        let newHistory =
                            history
                            |> Map.add aggregate (stream @ events)
                        return! loop newHistory
                  }

                loop Map.empty
            )

        let get () =
            mailbox.PostAndReply Get
        
        let getStream aggregate =
            mailbox.PostAndReply (fun reply -> GetStream (aggregate, reply))
        
        let append aggregate events =
            Append (aggregate, events)
            |> mailbox.Post
        
        {
            Get = get
            GetStream = getStream
            Append = append
        }
