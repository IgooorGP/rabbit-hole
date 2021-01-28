using System;
using System.Collections.Generic;
using System.Text;
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
        private IModel _subscriptionChannel;
        private IConnection _rabbitConnection;
        private readonly IConfigurationRoot _projectConfig;
        private readonly ILogger<RabbitBus> _logger;

        public RabbitBus(ILogger<RabbitBus> logger, ConnectionFactory rabbitConnectionFactory, IConfigurationRoot config)
        {
            _rabbitConnectionFactory = rabbitConnectionFactory;
            _projectConfig = config;
            _logger = logger;

            ConnectToRabbit();
        }
        /// <summary>
        /// Connects to a RabbitMQ broker with configured variables.
        /// </summary>
        private void ConnectToRabbit()
        {
            _logger.LogDebug("Setting RabbitMQ connection factory up...");

            _rabbitConnectionFactory.UserName = _projectConfig.GetValue<string>("RabbitMQ:UserName");
            _rabbitConnectionFactory.Password = _projectConfig.GetValue<string>("RabbitMQ:Password");
            _rabbitConnectionFactory.VirtualHost = _projectConfig.GetValue<string>("RabbitMQ:VHost");
            _rabbitConnectionFactory.HostName = _projectConfig.GetValue<string>("RabbitMQ:HostName");
            _rabbitConnectionFactory.Port = _projectConfig.GetValue<int>("RabbitMQ:Port");
            _rabbitConnectionFactory.AutomaticRecoveryEnabled = _projectConfig.GetValue<bool>("RabbitMQ:AutomaticRecoveryEnabled");

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
        public void Publish(Message message, string destination, Dictionary<string, object> headers = null, IModel channel = null)
        {
            // no channel was supplied, so a new one is created and then closed
            if (channel == null)
            {
                using var newChannel = _rabbitConnection.CreateModel();
                ChannelPublish(message, destination, headers, newChannel);

                return;
            }

            // if a channel is supplied, uses it (ch can be in transaction mode)
            ChannelPublish(message, destination, headers, channel);
        }
        public void Subscribe(string destination, Action<object, BasicDeliverEventArgs> callback)
        {
            _subscriptionChannel = _rabbitConnection.CreateModel();
            _subscriptionChannel.BasicQos(0, 1, false);
            var consumer = new EventingBasicConsumer(_subscriptionChannel);

            _logger.LogInformation("=^.^=: Waiting for messages...");

            // Creates a delegate (method pointer) to the callback param and adds this callback to be 
            // the handler for consumer.Received events
            consumer.Received += new EventHandler<BasicDeliverEventArgs>(callback);

            // Appends the consumer to the connection's channel
            _subscriptionChannel.BasicConsume(queue: destination, consumer: consumer);

            // sustain main thread
            _logger.LogInformation("=^.^=: Press [enter] to exit...");
            Console.ReadLine();
        }
        /// <summary>
        /// Applies publishing rules to queues and topics.
        /// </summary>
        private void ChannelPublish(Message message, string destination, Dictionary<string, object> headers, IModel channel)
        {
            // declares queue
            if (destination.Contains("/queue"))
                channel.QueueDeclare(destination, true, false, false);

            // message serialization
            var serializedMessage = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(serializedMessage);
            var finalHeaders = channel.CreateBasicProperties();

            finalHeaders.Headers = headers;
            finalHeaders.ContentType = "application/json";

            // queues -> default exchange handles it
            channel.BasicPublish(exchange: "",
                routingKey: destination,
                mandatory: true,
                basicProperties: finalHeaders,
                body: body);
        }
        /// <summary>
        /// Creates a new channel in transaction mode and returns to it the user.
        /// </summary>
        /// <returns>IModel - a channel in transaction mode</returns>
        public IModel BeginTx()
        {
            var channel = _rabbitConnection.CreateModel();
            channel.TxSelect();

            return channel;
        }
    }
}