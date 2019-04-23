using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amqp;
using Blun.MQ.Messages;

namespace Blun.MQ.AmqpNetLite
{
    internal sealed class AmqpNetLiteClientProxy : ClientProxy, IClientProxy
    {
        private Connection _connection;
        private Session _session;
        private readonly Address _adresse;

        public AmqpNetLiteClientProxy()
        {
            _adresse =new Address("amqp://guest:guest@localhost:5672");
        }

        public override void Dispose()
        {
            Disconnect();
        }
        
        public override Task<IMQResponse> SendAsync(IMQRequest request)
        {
            throw new NotImplementedException();
        }

        public override Task SetupQueueHandle(string queueName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public  void Connect()
        {
            _connection = new Connection(_adresse);
            _session = new Session(_connection);
        }

        public  void Disconnect()
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
