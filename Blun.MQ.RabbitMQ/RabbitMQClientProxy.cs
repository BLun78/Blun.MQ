using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blun.MQ;
using Blun.MQ.Abstractions;
using Blun.MQ.Abstractions.Message;
using Blun.MQ.Context;
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
        
        public override Task<MQResponse> SendAsync(MQRequest request)
        {
            byte[] messageBodyBytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request.Message));

            _channel.BasicPublish(request.MessageRoute, "", null, messageBodyBytes);

            return Task.FromResult((MQResponse)null);
        }

        public override void Connect()
        {
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public override void Disconnect()
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

        public override void SetupQueueHandle(IEnumerable<string> queues)
        {
            throw new NotImplementedException();
        }
    }
}
