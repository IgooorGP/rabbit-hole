namespace RabbitHole.Api
{
    public class ConfigurationRabbitMQ
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string VHost { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool AutomaticRecoveryEnabled { get; set; }
    }
}