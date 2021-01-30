namespace RabbitHole.Api
{
    public class ConfigurationRabbitMQ
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string VHost { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
        public bool AutomaticRecoveryEnabled { get; set; }
    }
}