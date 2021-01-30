using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitHole.Api;
using RabbitMQ.Client.Events;
using Serilog;
using Microsoft.Extensions.Logging;

namespace RabbitcsNewOrdersWorker
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }

        private static void SetupConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            Configuration = configuration;
        }

        private static void SetupSerilog()
        {
            // Serilog's global static property to be used with service collection as the ILogger of the project
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Config
            services.AddSingleton(services => Configuration);

            // Serilog
            services.AddLogging(builder => builder.AddSerilog(dispose: true));

            // RabbitMQ
            services.AddSingleton(Configuration.GetSection("RabbitMQ").Get<ConfigurationRabbitMQ>());
            services.AddSingleton<IRabbitBus, RabbitBus>();
        }

        public static void Callback(object sender, BasicDeliverEventArgs deliverArgs)
        {
            var body = deliverArgs.Body.ToArray();
            var channel = ((EventingBasicConsumer)sender).Model;
            var message = Encoding.UTF8.GetString(body);

            // some logic
            Log.Information("=^.^= Received {message}", message);  // Serilog's global logger

            // acks the msg
            channel.BasicAck(deliverArgs.DeliveryTag, false);
        }

        static void Main()
        {
            // Settings: appsettings.json and Serilog setup
            SetupConfiguration();
            SetupSerilog();

            // DI setup
            var services = new ServiceCollection();
            ConfigureServices(services);

            // Subscription
            var serviceProvider = services.BuildServiceProvider();
            var rabbitBus = serviceProvider.GetRequiredService<IRabbitBus>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Starting subscription...");
            rabbitBus.Subscribe("Consumer.NewOrdersConsumer.Topic.NewOrdersTopic", Callback);
        }
    }
}
