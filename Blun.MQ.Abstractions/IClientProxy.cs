using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blun.MQ
{
    public interface IClientProxy : IDisposable
    {
        event EventHandler<ReceiveMessageFromQueueEventArgs> MessageFromQueueReceived;
        void OnReceiveMessageFromQueueEventArgs(ReceiveMessageFromQueueEventArgs e);

        Task<string> SendAsync<T>(T message, string channel);

        void Connect();

        void Disconnect();

        void SetupQueueHandle(IEnumerable<string> queues);

    }
}