using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Context;
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

        public abstract Task<MQResponse> SendAsync(MQRequest request);
        
        public abstract void SetupQueueHandle(IEnumerable<string> queues, CancellationToken cancellationToken);

    }
#pragma warning restore CA1063 // Implement IDisposable Correctly
}