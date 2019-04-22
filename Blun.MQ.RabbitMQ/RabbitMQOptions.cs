namespace Blun.MQ.RabbitMQ
{
    // ReSharper disable once InconsistentNaming
    public class RabbitMQOptions
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }
        public int Port { get; set; }
        public string HostName { get; set; }
        
        // OR

        // Example: "amqp://user:pass@hostName:port/vhost";
        public string Uri { get; set; }
        
    }
}