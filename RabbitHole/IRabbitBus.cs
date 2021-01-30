using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitHole.Api
{
    /// <summary>
    /// The main message bus interface for a RabbitMQ broker: IRabbitHole.
    /// </summary>
    public interface IRabbitBus
    {
        void Publish(object message, string destination, Dictionary<string, object>? headers = null, IModel? channel = null);
        void Subscribe(string destination, Action<object?, BasicDeliverEventArgs> callback);
        IModel BeginTransactionalChannel();
        void CommitTransactionalChannel(IModel channel);
    }
}