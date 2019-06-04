using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Messages;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Blun.MQ
{
    internal abstract class Consumer : IDisposable
    {
        public event MessageReceivedEventHandler MessageReceived;

        public delegate void MessageReceivedEventHandler([NotNull] object sender,[NotNull] MessageReceivedEventArgs e);

        public abstract Task SetupQueueHandleAsync(
            [NotNull] KeyValuePair<string, IEnumerable<IMessageDefinition>> queuesAndMessages, 
            [NotNull] CancellationToken cancellationToken);
        
        public abstract Task StartListenerAsync([NotNull] CancellationToken cancellationToken);

        protected virtual void OnMessageReceived([NotNull] MessageReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        public abstract void Dispose();
    }
}
