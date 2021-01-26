using System.Collections.Generic;
using RabbitMQ.Client;

namespace RabbitHole.Api
{
    /// <summary>
    /// The main message bus interface for a RabbitMQ broker: IRabbitHole.
    /// </summary>
    public interface IRabbitBus
    {
        void Publish(Message message, string destination, Dictionary<string, object> headers = null, IModel channel = null);

        // void Subscribe<TEvent, TEventHandler>()
        //     where TEvent : Event
        //     where TEventHandler : IEventHandler;

        // void Unsubscribe<TEvent, TEventHandler>()
        //     where TEvent : Event
        //     where TEventHandler : IEventHandler;
    }
}