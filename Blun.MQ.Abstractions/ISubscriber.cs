using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Blun.MQ
{
    public interface ISubscriber : IDisposable
    {
        event EventHandler<ReceiveMessageFromQueueEventArgs> MessageFromQueueReceived;
        void SetupQueueHandle([NotNull, ItemNotNull] IDictionary<string, IEnumerable<IMessageDefinition>> queuesAndMessages, [NotNull] CancellationToken cancellationToken);

    }
}
