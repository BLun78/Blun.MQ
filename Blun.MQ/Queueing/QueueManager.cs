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

        private readonly ConsumerFactory _consumerFactory;
        private readonly ILogger<QueueManager> _logger;
        private readonly IDictionary<string, IEnumerable<IMessageDefinition>> _queueDictionary;
        private readonly IDictionary<string, ConsumerManager> _subscribers;

        public QueueManager(
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] ConsumerFactory consumerFactory)
        {
            _logger = loggerFactory.CreateLogger<QueueManager>();

            _consumerFactory = consumerFactory;
            _queueDictionary = CreateQueueDictionary();
            _subscribers = new SortedDictionary<string, ConsumerManager>(StringComparer.InvariantCulture);
        }

        public async Task SetupQueueHandleAsync([NotNull] CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            foreach (var subscriberDefinition in _queueDictionary)
            {
                var subscriber = await _consumerFactory.CreateSubscriberAsync(subscriberDefinition, cancellationToken).ConfigureAwait(false);
                _subscribers.Add(subscriberDefinition.Key, subscriber);
                var _ = subscriber.StartListenerAsync(cancellationToken);
            }
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