using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.AwsSQS.Client
{
    internal sealed class QueueInformations
    {
        private readonly AwsSQSClientDecorator _awsSqsClientDecorator;
        private readonly ILogger<QueueInformations> _logger;

        public QueueInformations(AwsSQSClientDecorator awsSqsClientDecorator, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<QueueInformations>();
            _awsSqsClientDecorator = awsSqsClientDecorator;
        }
    }
}
