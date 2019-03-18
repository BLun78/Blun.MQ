using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Messages;
using Blun.MQ.Messages.Strategies;
using Blun.MQ.Queueing;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.Hosting
{
    internal sealed class Subscriber : IDisposable
    {
        [NotNull] private readonly MessageInvoker _messageInvoker;
        [NotNull] private readonly IClientProxy _clientProxy;
        [NotNull] private readonly ILogger<Subscriber> _logger;
        [CanBeNull] private CancellationToken _cancellationToken;

        public Subscriber(
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] IClientProxy clientProxy,
            [NotNull] MessageInvoker messageInvoker)
        {
            _logger = loggerFactory.CreateLogger<Subscriber>();
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

        public void ClientProxyOnMessageFromQueueReceived([NotNull] object sender, [NotNull] ReceiveMessageFromQueueEventArgs e)
        {
            _ = _messageInvoker.HandleMessage(_cancellationToken, e);
        }

        public void Dispose()
        {
            _clientProxy.Dispose();
        }
    }
}
