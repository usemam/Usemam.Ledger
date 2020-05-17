namespace Usemam.EventSourcing.Proto.Infrastructure.EventStorage

open Usemam.EventSourcing.Proto.Infrastructure

module InMemoryStorage =
    
    type Msg<'Event> =
        private
        | Get of AsyncReplyChannel<EventResult<'Event>>
        | GetStream of EventSource * AsyncReplyChannel<EventResult<'Event>>
        | Append of EventEnvelope<'Event> list * AsyncReplyChannel<unit>
    
    let private streamFor source history =
        history |> List.filter (fun ee -> ee.Metadata.Source = source)

    let initialize () : EventStorage<'Event> =
        let mailbox =
            MailboxProcessor.Start(fun inbox ->
                let rec loop history =
                    async {
                        let! msg = inbox.Receive()
                        match msg with
                        | Get reply ->
                            history
                            |> Ok
                            |> reply.Reply
                            
                            return! loop history
                        | GetStream (source, reply) ->
                            history
                            |> streamFor source
                            |> Ok
                            |> reply.Reply
                            
                            return! loop history
                        | Append (events, reply) ->
                            reply.Reply ()
                            return! loop (history @ events) 
                    }
                
                loop []
            )
        {
            Get = fun () ->  mailbox.PostAndAsyncReply Get
            GetStream = fun eventSource ->
                mailbox.PostAndAsyncReply (fun reply -> (eventSource,reply) |> GetStream)
            Append = fun events ->
                mailbox.PostAndAsyncReply (fun reply -> (events,reply) |> Append)
        }