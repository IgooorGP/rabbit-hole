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
        private IModel _subscriptionChannel;
        private IConnection _rabbitConnection;
        private readonly ConfigurationRabbitMQ _projectConfig;
        private readonly ILogger<RabbitBus> _logger;

        public RabbitBus(ILogger<RabbitBus> logger, ConfigurationRabbitMQ config)
        {
            _rabbitConnectionFactory = new ConnectionFactory();
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
        public async Task PublishAsync(object message, string destination, Dictionary<string, object> headers = null, IModel channel = null)
        {
            // no channel was supplied, so a new one is created and then closed
            if (channel is null)
            {
                using var newChannel = _rabbitConnection.CreateModel();
                await ChannelPublish(message, destination, headers, newChannel);

                return;
            }

            // if a channel is supplied, uses it (ch can be in transaction mode)
            await ChannelPublish(message, destination, headers, channel);
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

            if (DestinationParsingTools.IsConsumerTopicDestination(destination))
            {
                var (consumerQueueName, topicName) = DestinationParsingTools
                    .ParseConsumerTopicDestination(destination);

                // requires a fanout exchange creation
                _subscriptionChannel.ExchangeDeclare(topicName, ExchangeType.Fanout, true);
                _subscriptionChannel.QueueDeclare(consumerQueueName, true, false, false);
                _subscriptionChannel.QueueBind(consumerQueueName, topicName, topicName);

                // for topic consumers -> destination is changed to /topicQ/TopicName/ConsumerName
                destination = consumerQueueName;
            }

            // Appends the consumer to the connection's channel
            _subscriptionChannel.BasicConsume(queue: destination, consumer: consumer);

            // sustain main thread
            _logger.LogInformation("=^.^=: Press [enter] to exit...");
            Console.ReadLine();
        }

        /// <summary>
        /// Applies publishing rules to queues and topics.
        /// </summary>
        private async Task ChannelPublish(object message, string destination, Dictionary<string, object> headers, IModel channel)
        {
            // message serialization
            var serializedMessage = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(serializedMessage);
            var finalHeaders = channel.CreateBasicProperties();

            finalHeaders.Headers = headers;
            finalHeaders.ContentType = "application/json";

            // topics ~ virtual topics in ActiveMQ
            if (DestinationParsingTools.IsTopicDestination(destination))
            {
                // run on background thread
                await Task.Run(() =>
                {
                    // requires a fanout exchange creation
                    channel.ExchangeDeclare(destination, ExchangeType.Fanout, true);

                    // fanout for topics -> all bound queues get the messages
                    channel.BasicPublish(
                        exchange: destination,
                        routingKey: "",  // no routing key for fanout exchanges
                        mandatory: true,
                        basicProperties: finalHeaders,
                        body: body);
                });
            }
            else
            {
                // run on background thread
                await Task.Run(() =>
                {
                    // queues
                    channel.QueueDeclare(destination, true, false, false);

                    // queues -> default exchange handles it
                    channel.BasicPublish(exchange: "",
                        routingKey: destination,
                        mandatory: true,
                        basicProperties: finalHeaders,
                        body: body);
                });
            }
        }

        /// <summary>
        /// Creates a new channel in transaction mode and returns to it the user.
        /// </summary>
        /// <returns>IModel - a channel in transaction mode</returns>
        public async Task<IModel> BeginTx()
        {
            // create ch on background thread
            var ch = await Task.Run(() =>
            {
                var channel = _rabbitConnection.CreateModel();
                channel.TxSelect();

                return channel;
            });

            return ch;
        }

        /// <summary>
        /// Commits a transaction present on a channel in transaction mode.
        /// </summary>
        /// <returns>IModel - a channel in transaction mode</returns>
        public async Task CommitTx(IModel channel)
        {
            // create ch on background thread
            await Task.Run(() =>
            {
                channel.TxCommit();
            });
        }
    }
}