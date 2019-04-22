using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blun.MQ.Messages;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Blun.MQ.RabbitMQ
{
    public class RabbitMqClientProxy : ClientProxy, IClientProxy
    {
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;

        public RabbitMqClientProxy()
        {
            _connectionFactory = new ConnectionFactory();
        }

        public override void Dispose()
        {
            _connection?.Dispose();
        }

        public override Task<IMQResponse> SendAsync(IMQRequest request)
        {
            byte[] messageBodyBytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request.Message));

            _channel.BasicPublish(request.MessageRoute, "", null, messageBodyBytes);

            return Task.FromResult((IMQResponse) null);
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

        public void SetupQueueHandle(IEnumerable<string> queues)
        {
            throw new NotImplementedException();
        }
    }
}