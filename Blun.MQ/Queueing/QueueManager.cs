using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Messages;
using Blun.MQ.Hosting;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.Queueing
{
    /// <summary>
    /// QueueManager creates the definitions for the queues
    /// </summary>
    internal sealed partial class QueueManager : IDisposable
    {
        /// <summary>
        /// CancellationToken will canceled if the host starts the Stop Method or Dispose
        /// </summary>
        private CancellationToken _cancellationToken;

        
        private readonly SubscriberFactory _subscriberFactory;
        private readonly ILogger<QueueManager> _logger;
        private readonly IDictionary<string, IEnumerable<IMessageDefinition>> _queueDictionary;
        private readonly IDictionary<string, Subscriber> _subscribers;

        public QueueManager(
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] SubscriberFactory subscriberFactory)
        {
            _logger = loggerFactory.CreateLogger<QueueManager>();
            
            _subscriberFactory = subscriberFactory;
            _queueDictionary = CreateQueueDictionary();
            _subscribers = new SortedDictionary<string, Subscriber>(StringComparer.InvariantCulture);
        }

        public Task SetupQueueHandle([NotNull] CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            
            foreach (var subscriberDefinition in _queueDictionary)
            {
                var subscriber = _subscriberFactory.CreateSubscriber(subscriberDefinition, cancellationToken);

                _subscribers.Add(subscriberDefinition.Key, subscriber);
            }
            
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber.Value?.Dispose();
            }
        }
        
        private static IDictionary<string, IEnumerable<IMessageDefinition>> CreateQueueDictionary()
        {
            var result = new SortedDictionary<string, IEnumerable<IMessageDefinition>>();
            var queueNames = QueueManager.MessageDefinitions
                .OrderBy(o => o.QueueName, StringComparer.InvariantCulture)
                .Select(s => s.QueueName)
                .Distinct(StringComparer.InvariantCulture);

            foreach (var queueName in queueNames)
            {
                var messageDefinitions = QueueManager.MessageDefinitions
                    .Where(w => w.QueueName.Equals(queueName, StringComparison.InvariantCulture))
                    .OrderBy(o => o.QueueName, StringComparer.InvariantCulture);
                result.Add(queueName, messageDefinitions);
            }

            return result;
        }

    }
}
