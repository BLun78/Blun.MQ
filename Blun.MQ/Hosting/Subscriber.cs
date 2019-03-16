using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.Hosting
{
    internal sealed class Subscriber : ISubscriber, IDisposable
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
        public void SetupQueueHandle(IDictionary<string, IEnumerable<IMessageDefinition>> queuesAndMessages, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
