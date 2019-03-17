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
    internal sealed partial class QueueManager : IDisposable
    {
        private CancellationToken _cancellationToken;

        private readonly ControllerProvider _controllerProvider;
        private readonly SubscriberFactory _subscriberFactory;
        private readonly ILogger<QueueManager> _logger;
        private readonly IDictionary<string, IEnumerable<IMessageDefinition>> _queueDictionary;
        private readonly IDictionary<string, Subscriber> _subscribers;

        public QueueManager(
            [NotNull] ControllerProvider controllerProvider,
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] SubscriberFactory subscriberFactory)
        {
            _logger = loggerFactory.CreateLogger<QueueManager>();
            _controllerProvider = controllerProvider;
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

        private void ClientProxyOnMessageFromQueueReceived(object sender, [NotNull] ReceiveMessageFromQueueEventArgs e)
        {
            if (_cancellationToken == null) throw new InvalidOperationException("SetupQueueHandle() wasn't run!");
            _ = Task.Run(() =>
           {
               using (_logger.BeginScope("ReceiveMessageFromQueue"))
               {
                   var controller = _controllerProvider.GetController(e);
                   var messageDefinition = FindControllerByKey[e.Key];
                   if (messageDefinition.MethodInfo.ReturnType == null)
                   {
                       messageDefinition.MethodInfo.Invoke(controller, new object[] { e.Message });
                   }
                   if (messageDefinition.MethodInfo.ReturnType?.GetNestedType(nameof(IMQResponse)) != null)
                   {
                       messageDefinition.MethodInfo.Invoke(controller, new object[] { e.Message });
                   }
               }
           }, _cancellationToken);
        }

        private static IDictionary<string, IEnumerable<IMessageDefinition>> CreateQueueDictionary()
        {
            var result = new SortedDictionary<string, IEnumerable<IMessageDefinition>>();
            var queueNames = QueueManager.MessageDefinitions
                .OrderBy(o => o.QueueName, StringComparer.Ordinal)
                .Select(s => s.QueueName)
                .Distinct(StringComparer.Ordinal);

            foreach (var queueName in queueNames)
            {
                var messageDefinitions = QueueManager.MessageDefinitions
                    .Where(w => w.QueueName.Equals(queueName, StringComparison.Ordinal))
                    .OrderBy(o => o.QueueName, StringComparer.Ordinal);
                result.Add(queueName, messageDefinitions);
            }

            return result;
        }

    }
}
