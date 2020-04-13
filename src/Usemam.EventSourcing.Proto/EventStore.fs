namespace Usemam.EventSourcing.Proto

module EventStore =

    type EventStore<'Event> =
        {
            Get: unit -> 'Event list
            Append: 'Event list -> unit
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
