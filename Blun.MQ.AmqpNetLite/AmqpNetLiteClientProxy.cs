using System;
using System.Threading.Tasks;
using Amqp;
using Blun.MQ;

namespace Blun.MQ.AmqpNetLite
{
    public class AmqpNetLiteClientProxy : IClientProxy
    {
        private Connection _connection;
        private Session _session;
        private readonly Address _adresse;

        public AmqpNetLiteClientProxy()
        {
            _adresse =new Address("amqp://guest:guest@localhost:5672");
        }

        public void Dispose()
        {
            Disconnect();
        }

        public Task<string> SendAsync<T>(T message, string channel)
        {
            throw new NotImplementedException();
        }

        public void Connect()
        {
            _connection = new Connection(_adresse);
            _session = new Session(_connection);
        }

        public void Disconnect()
        {
            if (!_session.IsClosed)
            {
                _session.Close();
            }
            if (!_connection.IsClosed)
            {
                _connection.Close();
            }
        }
    }
}
