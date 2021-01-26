
using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitHole.Api;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitcsNewOrdersWorker
{
    class Program
    {
        private static IConfigurationRoot BuildConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            return configuration;
        }

        public static void Callback(object sender, BasicDeliverEventArgs deliverArgs)
        {
            var body = deliverArgs.Body.ToArray();
            var channel = ((EventingBasicConsumer)sender).Model;
            var message = Encoding.UTF8.GetString(body);

            // some logic
            Console.WriteLine(" [x] Received {0}", message);

            // acks the msg
            channel.BasicAck(deliverArgs.DeliveryTag, false);
        }

        static void Main(string[] args)
        {
            var rabbitConnectionFactory = new ConnectionFactory();
            var config = BuildConfiguration();
            var rabbitBus = new RabbitBus(rabbitConnectionFactory, config);

            rabbitBus.Subscribe("/queue/test-destination", Callback);
        }
    }
}
