using System;
using System.Collections.Generic;
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
        

        void SetupQueueHandle([NotNull, ItemNotNull] IDictionary<string, IEnumerable<IMessageDefinition>> queuesAndMessages,[NotNull] CancellationToken cancellationToken);

    }
}