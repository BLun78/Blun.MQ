using System;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Messages;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Blun.MQ
{
    public interface IClientProxy : IDisposable
    {
        event EventHandler<ReceiveMessageFromQueueEventArgs> MessageFromQueueReceived;

        Task<IMQResponse> SendAsync(IMQRequest request);
        

        Task SetupQueueHandle([NotNull, ItemNotNull] string queueName,[NotNull] CancellationToken cancellationToken);

    }
}