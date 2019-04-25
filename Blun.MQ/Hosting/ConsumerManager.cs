﻿using System;
using System.Collections.Generic;
using System.Threading;
using Blun.MQ.Messages;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.Hosting
{
    internal sealed class ConsumerManager : IDisposable
    {
        [NotNull] private readonly MessageInvoker _messageInvoker;
        [NotNull] private readonly Consumer _consumer;
        [NotNull] private readonly ILogger<ConsumerManager> _logger;
        [CanBeNull] private CancellationToken _cancellationToken;

        public ConsumerManager(
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] Consumer consumer,
            [NotNull] MessageInvoker messageInvoker)
        {
            _logger = loggerFactory.CreateLogger<ConsumerManager>();
            _messageInvoker = messageInvoker;
            _consumer = consumer;
        }

        public void SetupQueueHandle(
            [NotNull] KeyValuePair<string, IEnumerable<IMessageDefinitionResponseInfo>> queuesAndMessages,
            [NotNull] CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _consumer.SetupQueueHandleAsync(queuesAndMessages.Key, cancellationToken);
            _consumer.MessageReceived += ClientProxyOnMessageFromQueueReceived;
        }

        private void ClientProxyOnMessageFromQueueReceived([NotNull] object sender, [NotNull] MessageReceivedEventArgs eventArgs)
        {
            _ = _messageInvoker.HandleMessage(_cancellationToken, eventArgs);
        }

        public void Dispose()
        {
            _consumer.Dispose();
        }
    }
}
