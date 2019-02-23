using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blun.MQ
{
    public abstract class ClientProxy : IClientProxy
    {
        public abstract void Dispose();

        public event EventHandler<ReceiveMessageFromQueueEventArgs> MessageFromQueueReceived;
        public virtual void OnReceiveMessageFromQueueEventArgs(ReceiveMessageFromQueueEventArgs e)
        {
            MessageFromQueueReceived?.Invoke(this, e);
        }

        public abstract Task<string> SendAsync<T>(T message, string channel);

        public abstract void Connect();

        public abstract void Disconnect();

        public abstract void SetupQueueHandle(IEnumerable<string> queues);
    }
}