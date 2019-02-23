using System;
using System.Collections.Generic;
using System.Threading.Tasks;
// ReSharper disable CheckNamespace

namespace Blun.MQ
{
#pragma warning disable CA1063 // Implement IDisposable Correctly
    public abstract class ClientProxy : IClientProxy, IDisposable

    {
        public abstract void Dispose();

        public event EventHandler<ReceiveMessageFromQueueEventArgs> MessageFromQueueReceived;
        protected void OnReceiveMessageFromQueueEventArgs(ReceiveMessageFromQueueEventArgs e)
        {
            MessageFromQueueReceived?.Invoke(this, e);
        }

        public abstract Task<string> SendAsync<T>(T message, string channel);

        public abstract void Connect();

        public abstract void Disconnect();

        public abstract void SetupQueueHandle(IEnumerable<string> queues);
    }
#pragma warning restore CA1063 // Implement IDisposable Correctly
}