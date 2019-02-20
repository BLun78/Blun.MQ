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
        private AmazonSQSClient _amazonSqsClient;
        private readonly AmazonSQSConfig _amazonSqsConfig;
        private readonly List<QueueuHandle> _queueHandles;

        public AwsSQSClientProxy()
        {
            _queueHandles = new List<QueueuHandle>();
            _amazonSqsConfig = new AmazonSQSConfig();
        }
        
        public override async Task<string> SendAsync<T>(T message, string queue)
        {
            var request = new SendMessageRequest(_amazonSqsConfig.ServiceURL + queue,
                JsonConvert.SerializeObject(message));

            SendMessageResponse result = await _amazonSqsClient.SendMessageAsync(request);

            return result.MessageId;
        }
        
        public override void Connect()
        {
            _amazonSqsClient = new AmazonSQSClient(_amazonSqsConfig);
        }

        public override void Disconnect()
        {
            Dispose();
        }

        public override void SetupQueueHandle(IEnumerable<string> queues)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            _amazonSqsClient?.Dispose();
        }

    }
}
