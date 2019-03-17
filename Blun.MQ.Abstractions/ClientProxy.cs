using System;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Messages;

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

        public abstract Task<IMQResponse> SendAsync(IMQRequest request);

        public abstract void SetupQueueHandle(string queueName, CancellationToken cancellationToken);

    }
#pragma warning restore CA1063 // Implement IDisposable Correctly
}