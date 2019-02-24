﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Context;
using Blun.MQ.Messages;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.AwsSQS.Client
{
    // ReSharper disable once InconsistentNaming
    internal class AwsSQSClientProxy : ClientProxy, IClientProxy
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDictionary<string, QueueHandle> _queueHandles;
        private readonly ILogger<AwsSQSClientProxy> _logger;

        public AwsSQSClientProxy(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<AwsSQSClientProxy>();
            _queueHandles = new SortedDictionary<string, QueueHandle>(StringComparer.Ordinal);
        }

        public override async Task<MQResponse> SendAsync(MQRequest mqRequest)
        {
            var handle = GetQueueHandle(mqRequest.QueueRoute);

            var result = await handle.SendAsync(mqRequest).ConfigureAwait(false);
            return result;
        }

        public override void SetupQueueHandle(IDictionary<string, IEnumerable<IMessageDefinition>> queuesAndMessages, CancellationToken cancellationToken)
        {
            foreach (var queuesAndMessage in queuesAndMessages)
            {
                var newQueueHandle = new QueueHandle(queuesAndMessage.Value, _loggerFactory, cancellationToken);
                newQueueHandle.MessageFromQueueReceived += OnMessageFromQueueReceived;
                newQueueHandle.CreateQueueListener();
                _queueHandles.Add(queuesAndMessage.Key, newQueueHandle);
            }
        }

        public override void Dispose()
        {
            foreach (var queueHandle in _queueHandles)
            {
                queueHandle.Value?.Dispose();
            }
        }

        private void OnMessageFromQueueReceived(object sender, ReceiveMessageFromQueueEventArgs e)
        {
            OnReceiveMessageFromQueueEventArgs(e);
        }

        private QueueHandle GetQueueHandle(string queue)
        {
            if (!_queueHandles.ContainsKey(queue))
            {
                throw new KeyNotFoundException($"QueueHandle not found [{queue}]");
            }

            var handle = _queueHandles[queue];
            return handle;
        }

    }
}
