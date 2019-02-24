using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Amqp;
using Blun.MQ;
using Blun.MQ.Context;
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
        
        public override Task<MQResponse> SendAsync(MQRequest request)
        {
            throw new NotImplementedException();
        }

        public override void Connect()
        {
            _connection = new Connection(_adresse);
            _session = new Session(_connection);
        }

        public override void Disconnect()
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

        public override void SetupQueueHandle(IEnumerable<string> queues)
        {
            throw new NotImplementedException();
        }
    }
}
