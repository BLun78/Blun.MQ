using System;
using Amazon.SQS;
using Blun.MQ;

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
