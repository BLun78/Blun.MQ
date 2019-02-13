using System;
using BLun.MQ.Abstraction;
using RabbitMQ.Client;

namespace Blun.MQ.RabbitMQ
{
    public class RabbitMQClientProxy : IClientProxy
    {
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _connection;

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

        public void Connect()
        {
            _connection = _connectionFactory.CreateConnection();
        }

        public void Disconnect()
        {
            if (_connection != null && _connection.IsOpen)
            {
                _connection.Close();
            }
        }
    }
}
