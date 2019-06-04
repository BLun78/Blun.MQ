﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Messages;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.Hosting
{
    // ReSharper disable once ClassNeverInstantiated.Global
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

        public Task SetupQueueHandle(
            [NotNull] KeyValuePair<string, IEnumerable<IMessageDefinition>> queuesAndMessages,
            [NotNull] CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _consumer.MessageReceived += ClientProxyOnMessageFromQueueReceived;
            return _consumer.SetupQueueHandleAsync(queuesAndMessages, cancellationToken);
        }

        public Task StartListenerAsync([NotNull] CancellationToken cancellationToken)
        {
            return _consumer.StartListenerAsync(cancellationToken);
        }

        private void ClientProxyOnMessageFromQueueReceived([NotNull] object sender,
            [NotNull] MessageReceivedEventArgs eventArgs)
        {
            _ = _messageInvoker.HandleMessage(eventArgs, _cancellationToken);
        }

        public void Dispose()
        {
            _consumer.Dispose();
        }
    }
}