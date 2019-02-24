using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blun.MQ.Context;
using Blun.MQ.Messages;

// ReSharper disable once CheckNamespace
namespace Blun.MQ
{
    public interface IClientProxy : IDisposable
    {
        event EventHandler<ReceiveMessageFromQueueEventArgs> MessageFromQueueReceived;

        Task<MQResponse> SendAsync(MQRequest request);

        void Connect();

        void Disconnect();

        void SetupQueueHandle(IEnumerable<string> queues);

    }
}