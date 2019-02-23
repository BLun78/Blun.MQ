﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Blun.MQ;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Blun.MQ.AwsSQS
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

        public override async Task<string> SendAsync<T>(T message, string queue)
        {
            var handle = GetQueueHandle(queue);

            var result = await handle.SendAsync(message).ConfigureAwait(false);

            return result.MessageId;
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

        public override void Connect()
        {

        }

        public override void Disconnect()
        {
            Dispose();
        }

        public override void SetupQueueHandle(IEnumerable<string> queues)
        {
            foreach (var queue in queues)
            {
                var newQueueHandle = new QueueHandle(queue, _loggerFactory);
                newQueueHandle.MessageFromQueueReceived += OnMessageFromQueueReceived;
                _queueHandles.Add(queue, newQueueHandle);
            }
        }

        private void OnMessageFromQueueReceived(object sender, ReceiveMessageFromQueueEventArgs e)
        {
            OnReceiveMessageFromQueueEventArgs(e);
        }

        public override void Dispose()
        {

        }

       
    }
}
