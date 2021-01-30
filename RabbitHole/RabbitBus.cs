using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitHole.Api
{
    /// <summary>
    /// Message bus implementation for RabbitMQ brokers: the Rabbit Hole.
    /// </summary>
    public class RabbitBus : IRabbitBus
    {
        private readonly ConnectionFactory _rabbitConnectionFactory;
        private readonly IConnection _rabbitConnection;
        private readonly ConfigurationRabbitMQ _projectConfig;
        private readonly ILogger<RabbitBus> _logger;

        public RabbitBus(ILogger<RabbitBus> logger, ConfigurationRabbitMQ config)
        {
            _rabbitConnectionFactory = new ConnectionFactory();
            _projectConfig = config;
            _logger = logger;

            _logger.LogDebug("Setting RabbitMQ connection factory up...");

            _rabbitConnectionFactory.UserName = _projectConfig.UserName;
            _rabbitConnectionFactory.Password = _projectConfig.Password;
            _rabbitConnectionFactory.VirtualHost = _projectConfig.VHost;
            _rabbitConnectionFactory.HostName = _projectConfig.HostName;
            _rabbitConnectionFactory.Port = _projectConfig.Port;
            _rabbitConnectionFactory.AutomaticRecoveryEnabled = _projectConfig.AutomaticRecoveryEnabled;

            _logger.LogDebug("Connecting to RabbitMQ broker...");
            _rabbitConnection = _rabbitConnectionFactory.CreateConnection();

            _logger.LogDebug("=^.^=: Connected to RabbitMQ!");
        }

        /// <summary>
        /// Publishes a message to a destination (queue or topic).
        /// </summary>
        /// <param name="message">Message to be published</param>
        /// <param name="destination">Destination name</param>
        /// <param name="headers">Collection of headers (if any)</param>
        public void Publish(object message, string destination, Dictionary<string, object>? headers = null, IModel? channel = null)
        {
            // if a channel is supplied, uses it (ch can be in transaction mode)
            ChannelPublish(message, destination, headers, channel);
        }

        public void Subscribe(string destination, Action<object?, BasicDeliverEventArgs> callback)
        {
            _logger.LogInformation("=^.^=: Creating a new channel for the subscription...");
            var (_, channel) = SubscriptionStrategy.SubscribeConsumer(callback, destination, _rabbitConnection, _logger);

            _logger.LogInformation("=^.^=: Press [enter] to exit...");
            Console.ReadLine();

            _logger.LogInformation("=^.^=: Bye!");
            channel.Dispose();
        }

        /// <summary>
        /// Applies publishing rules to queues and topics.
        /// </summary>
        private void ChannelPublish(object message, string destination, Dictionary<string, object>? headers, IModel? channel = null)
        {
            // new channel if necessary for publishing
            var createdNewChannel = channel is null;
            var ch = channel ?? _rabbitConnection.CreateModel();

            // msg headers
            var finalHeaders = ch.CreateBasicProperties();
            finalHeaders.Headers = headers;
            finalHeaders.ContentType = "application/json";

            // msg body
            var serializedMessage = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(serializedMessage);

            // selects appropriate publishing strategy
            PublishingStrategy.Send(body, finalHeaders, destination, ch);

            // only dipose if the the lib created a new channel here
            if (createdNewChannel) ch.Dispose();
        }

        /// <summary>
        /// Creates a new channel in transaction mode and returns to it the user.
        /// </summary>
        /// <returns>IModel - a channel in transaction mode</returns>
        public IModel BeginTransactionalChannel()
        {
            var channel = _rabbitConnection.CreateModel();
            channel.TxSelect();

            return channel;
        }

        /// <summary>
        /// Commits a transaction present on a channel in transaction mode.
        /// </summary>
        /// <returns>IModel - a channel in transaction mode</returns>
        public void CommitTransactionalChannel(IModel channel)
        {
            channel.TxCommit();
        }
    }
}