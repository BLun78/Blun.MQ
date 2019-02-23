using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Blun.MQ;
using Newtonsoft.Json;

namespace Blun.MQ.AwsSQS
{
    internal class AwsSQSClientProxy : ClientProxy, IClientProxy
    {
        private readonly IDictionary<string, QueueHandle> _queueHandles;

        public AwsSQSClientProxy()
        {
            _queueHandles = new SortedDictionary<string, QueueHandle>(StringComparer.Ordinal);
        }

        public override async Task<string> SendAsync<T>(T message, string queue)
        {
            var handle = GetQueueHandle(queue);

            var result = await handle.SendAsync(message, queue);

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
                _queueHandles.Add(queue, new QueueHandle(queue, OnReceiveMessageFromQueueEventArgs()));
            }
        }
        
        public override void Dispose()
        {

        }

    }
}
