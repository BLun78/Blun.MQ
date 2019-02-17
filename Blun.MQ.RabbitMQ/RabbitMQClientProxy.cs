using System;
using System.Threading.Tasks;
using Blun.MQ;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Blun.MQ.RabbitMQ
{
    public class RabbitMQClientProxy : IClientProxy
    {
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;

        public RabbitMQClientProxy()
        {
            _connectionFactory = new ConnectionFactory();
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
            }
        }

        public void Setup()
        {

        }

        public Task<string> SendAsync<T>(T message, string channel)
        {
            byte[] messageBodyBytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

            _channel.BasicPublish(channel, "", null, messageBodyBytes);
            
            return Task.FromResult("");
        }

        public void Connect()
        {
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public void Disconnect()
        {
            if (_channel != null && _channel.IsOpen)
            {
                _channel.Close();
            }
            if (_connection != null && _connection.IsOpen)
            {
                _connection.Close();
            }
        }
    }
}
