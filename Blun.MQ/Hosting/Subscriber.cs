using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Blun.MQ.Messages;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.Hosting
{
    internal sealed class Subscriber : IDisposable
    {
        private readonly IClientProxy _clientProxy;
        private readonly ILogger<Subscriber> _logger;

        public Subscriber(
            ILoggerFactory loggerFactory,
            IClientProxy clientProxy)
        {
            _clientProxy = clientProxy;
            _logger = loggerFactory.CreateLogger< Subscriber>();
        }

        public event EventHandler<ReceiveMessageFromQueueEventArgs> MessageFromQueueReceived;
        public void SetupQueueHandle(KeyValuePair<string, IEnumerable<IMessageDefinition>> queuesAndMessages, CancellationToken cancellationToken)
        {
            _clientProxy.SetupQueueHandle(queuesAndMessages.Key, cancellationToken);
            _clientProxy.MessageFromQueueReceived += ClientProxyOnMessageFromQueueReceived;
        }

        public void ClientProxyOnMessageFromQueueReceived(object sender, ReceiveMessageFromQueueEventArgs e)
        {
            throw new NotImplementedException();
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
