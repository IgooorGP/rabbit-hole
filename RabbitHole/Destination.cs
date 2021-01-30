using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RabbitHole.Api;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitHole
{
    public static class PublishingStrategy
    {
        public static void Send(byte[] body, IBasicProperties headers, string destination, IModel channel)
        {
            if (DestinationParsingTools.IsTopicDestination(destination))
            {
                channel.ExchangeDeclare(destination, ExchangeType.Fanout, true);
                channel.BasicPublish(
                    exchange: destination,  // fanout for the queue
                    routingKey: "",  // no routing key for fanout exchanges
                    mandatory: true,
                    basicProperties: headers,
                    body: body);
            }
            else
            {
                channel.QueueDeclare(destination, true, false, false);
                channel.BasicPublish(exchange: "",  // queues -> default exchange handles it
                    routingKey: destination,
                    mandatory: true,
                    basicProperties: headers,
                    body: body);
            }
        }
    }

    public static class SubscriptionStrategy
    {
        public static Tuple<EventingBasicConsumer, IModel> SubscribeConsumer(Action<object?, BasicDeliverEventArgs> callback,
            string destination,
            IConnection rabbitConn,
            ILogger logger)
        {
            var channel = rabbitConn.CreateModel();
            var finalDestination = string.Empty;

            // subscription setup
            channel.BasicQos(0, 1, false); // TODO: improve QoS setup

            if (DestinationParsingTools.IsConsumerTopicDestination(destination))
                finalDestination = ApplySetupTopic(destination, channel, logger);
            else
                finalDestination = ApplySetupQueue(destination, channel, logger);

            logger.LogInformation("=^.^=: Appending new consumer to the channel...");
            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += new EventHandler<BasicDeliverEventArgs>(callback);
            channel.BasicConsume(queue: finalDestination, consumer: consumer);

            logger.LogInformation("=^.^=: Waiting for messages...");
            return new Tuple<EventingBasicConsumer, IModel>(consumer, channel);
        }

        public static string ApplySetupTopic(string destination, IModel channel, ILogger logger)
        {
            logger.LogInformation("=^.^=: Applying setup for topics...");

            var (consumerQueueName, topicName) = DestinationParsingTools
                .ParseConsumerTopicDestination(destination);

            // requires a fanout exchange creation
            channel.ExchangeDeclare(topicName, ExchangeType.Fanout, true);
            channel.QueueDeclare(consumerQueueName, true, false, false);
            channel.QueueBind(consumerQueueName, topicName, topicName);

            // for topic consumers -> destination is changed to /topicQ/TopicName/ConsumerName-
            return consumerQueueName;
        }

        public static string ApplySetupQueue(string destination, IModel channel, ILogger logger)
        {
            logger.LogInformation("=^.^=: Applying setup for queues...");

            // default exchange always exists and new queues are always bound to it!
            channel.QueueDeclare(destination, true, false, false);

            // destination name here remains the same
            return destination;
        }
    }

    public static class DestinationParsingTools
    {
        public static Regex CONSUMER_TOPIC_PATTERN = new Regex(@"^Consumer\.(?<consumerName>.+)\.Topic\.(?<topicName>.+)", RegexOptions.IgnoreCase);
        public static Regex TOPIC_PATTERN = new Regex(@"^\/topic\/(?<topicName>.+)", RegexOptions.IgnoreCase);
        public static Regex QUEUE_PATTERN = new Regex(@"^\/queue\/(?<queueName>.+)", RegexOptions.IgnoreCase);

        public static bool IsQueueDestination(string destination)
        {
            return QUEUE_PATTERN.Match(destination).Success;
        }

        public static bool IsTopicDestination(string destination)
        {
            return TOPIC_PATTERN.Match(destination).Success;
        }

        public static bool IsConsumerTopicDestination(string destination)
        {
            return CONSUMER_TOPIC_PATTERN.Match(destination).Success;
        }

        public static Tuple<string, string> ParseConsumerTopicDestination(string destination)
        {
            var match = CONSUMER_TOPIC_PATTERN.Match(destination);

            if (!match.Success)
                throw new ParseDestinationException($"Unable to parse consumer topic destination: {destination}");

            var consumerName = match.Groups["consumerName"].Value;  // -> XPTO
            var noPrefixTopicName = match.Groups["topicName"].Value;  // -> TopicName

            var prefixedTopicName = $"/topic/{noPrefixTopicName}";  // -> /topic/TopicName
            var consumerQueueName = $"/topicQ/{noPrefixTopicName}/{consumerName}"; // -> /topicQ/TopicName/XPTO

            return new Tuple<string, string>(consumerQueueName, prefixedTopicName);
        }
    }
}