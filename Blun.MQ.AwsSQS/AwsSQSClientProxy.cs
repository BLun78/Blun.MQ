using System;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Blun.MQ;
using Newtonsoft.Json;

namespace Blun.MQ.AwsSQS
{
    public class AwsSQSClientProxy : IClientProxy
    {
        private AmazonSQSClient _amazonSqsClient;
        private readonly AmazonSQSConfig _amazonSqsConfig;

        public AwsSQSClientProxy()
        {
            _amazonSqsConfig = new AmazonSQSConfig();
        }

        public void Dispose()
        {
            if (_amazonSqsClient != null)
            {
                _amazonSqsClient.Dispose();
            }
        }

        public async Task<string> SendAsync<T>(T message, string channel)
        {
            var request = new SendMessageRequest();
            request.QueueUrl = _amazonSqsConfig.ServiceURL + channel;
            request.MessageBody = JsonConvert.SerializeObject(message);
            
            SendMessageResponse result = await _amazonSqsClient.SendMessageAsync(request);

            return result.MessageId;
        }

        public void Connect()
        {
            _amazonSqsClient = new AmazonSQSClient(_amazonSqsConfig);
        }

        public void Disconnect()
        {
            this.Dispose();
        }
    }
}
