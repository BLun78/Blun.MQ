using System;
using System.Collections.Generic;
using System.Threading;
using Blun.MQ.Messages;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.Hosting
{
    internal sealed class Consumer : IDisposable
    {
        [NotNull] private readonly MessageInvoker _messageInvoker;
        [NotNull] private readonly IClientProxy _clientProxy;
        [NotNull] private readonly ILogger<Consumer> _logger;
        [CanBeNull] private CancellationToken _cancellationToken;

        public Consumer(
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] IClientProxy clientProxy,
            [NotNull] MessageInvoker messageInvoker)
        {
            _logger = loggerFactory.CreateLogger<Consumer>();
            _messageInvoker = messageInvoker;
            _clientProxy = clientProxy;
        }

        public void SetupQueueHandle(
            [NotNull] KeyValuePair<string, IEnumerable<IMessageDefinition>> queuesAndMessages,
            [NotNull] CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _clientProxy.SetupQueueHandle(queuesAndMessages.Key, cancellationToken);
            _clientProxy.MessageFromQueueReceived += ClientProxyOnMessageFromQueueReceived;
        }

        private void ClientProxyOnMessageFromQueueReceived([NotNull] object sender, [NotNull] ReceiveMessageFromQueueEventArgs eventArgs)
        {
            _ = _messageInvoker.HandleMessage(_cancellationToken, eventArgs);
        }

        public void Dispose()
        {
            _clientProxy.Dispose();
        }
    }
}
