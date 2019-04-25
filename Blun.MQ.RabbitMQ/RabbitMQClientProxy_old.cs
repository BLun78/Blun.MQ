using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Messages;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Blun.MQ.RabbitMQ
{
    public class RabbitMqClientProxy 
    {
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;

        public RabbitMqClientProxy()
        {
            _connectionFactory = new ConnectionFactory();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        public Task<IMQResponse> SendAsync(IMQRequest request)
        {
            byte[] messageBodyBytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request.Message));

            _channel.BasicPublish(request.MessageRoute, "", null, messageBodyBytes);

            return Task.FromResult((IMQResponse) null);
        }

        public Task SetupQueueHandle(string queueName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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