using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blun.MQ
{
    public interface IClientProxy : IDisposable
    {
        event EventHandler<ReceiveMessageFromQueueEventArgs> MessageFromQueueReceived;

        Task<string> SendAsync<T>(T message, MQRequest request);

        void Connect();

        void Disconnect();

        void SetupQueueHandle(IEnumerable<string> queues);

    }
}