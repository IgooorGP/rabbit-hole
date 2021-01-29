using System;
using System.Text.RegularExpressions;

namespace RabbitHole
{
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

        public static string ParseQueueDestination(string destination)
        {
            var match = QUEUE_PATTERN.Match(destination);

            if (!match.Success)
                throw new ParseDestinationException($"Unable to parse queue destination: {destination}");

            return match.Groups["queueName"].Value;
        }

        public static string ParseTopicDestination(string destination)
        {
            var match = TOPIC_PATTERN.Match(destination);

            if (!match.Success)
                throw new ParseDestinationException($"Unable to parse topic destination: {destination}");

            return match.Groups["topicName"].Value;
        }

        public static Tuple<string, string> ParseConsumerTopicDestination(string destination)
        {
            var match = CONSUMER_TOPIC_PATTERN.Match(destination);

            if (!match.Success)
                throw new ParseDestinationException($"Unable to parse consumer topic destination: {destination}");

            var consumerName = match.Groups["consumerName"].Value;
            var topicName = match.Groups["topicName"].Value;

            return new Tuple<string, string>(consumerName, topicName);
        }
    }
}