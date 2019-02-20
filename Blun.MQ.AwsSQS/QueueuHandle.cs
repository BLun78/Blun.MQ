using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;

namespace Blun.MQ.AwsSQS
{
    internal class QueueHandle
    {
        private readonly string _queueName;
        private AmazonSQSClient _amazonSqsClient;
        private readonly AmazonSQSConfig _amazonSqsConfig;
        
        public QueueHandle(string queueName)
        {
            _queueName = queueName;
            _amazonSqsConfig = new AmazonSQSConfig();
        }

        public async Task<SendMessageResponse> SendAsync<T>(T message, string queue)
        {
            var request = new SendMessageRequest(_amazonSqsConfig.ServiceURL + queue,
                JsonConvert.SerializeObject(message));

            SendMessageResponse result = await _amazonSqsClient.SendMessageAsync(request);

            return result;
        }

        public void Connect()
        {
            _amazonSqsClient = new AmazonSQSClient(_amazonSqsConfig);
        }

    }
}
