namespace Usemam.EventSourcing.Proto.Infrastructure

module EventSourced =

    type EventSourcedConfig<'Event, 'Command, 'Query> =
        {
            EventStoreInit : EventStorage<'Event> -> EventStore<'Event>
            EventStorageInit : unit -> EventStorage<'Event>
            CommandHandlerInit : EventStore<'Event> -> CommandHandler<'Command>
            QueryHandler : QueryHandler<'Query>
            EventListenerInit : unit -> EventListener<'Event>
            EventHandlers : EventHandler<'Event> list
        }
    
    type EventSourced<'Event, 'Command, 'Query> (config : EventSourcedConfig< 'Event, 'Command, 'Query>) =

        let eventStorage = config.EventStorageInit()
        let eventStore = config.EventStoreInit eventStorage
        let commandHandler = config.CommandHandlerInit eventStore
        let eventListener = config.EventListenerInit()

        do
            eventStore.OnEvents.Add eventListener.Notify
            config.EventHandlers |> List.iter eventListener.Subscribe
        
        member _.HandleCommand eventSource command =
            commandHandler.Handle eventSource command
        
        member _.HandleQuery query =
            config.QueryHandler.Handle query
        
        member _.GetAllEvents () = eventStore.Get()

        member _.GetStream eventSource =
            eventStore.GetStream eventSource