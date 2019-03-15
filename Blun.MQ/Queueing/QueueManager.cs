using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Hosting;
using Blun.MQ.Messages;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.Queueing
{
    internal sealed partial class QueueManager : IDisposable
    {
        private readonly ControllerProvider _controllerProvider;
        private IEnumerable<IClientProxy> _clientProxies;
        private CancellationToken _cancellationToken;
        private ILogger<QueueManager> _logger;

        public QueueManager(ControllerProvider controllerProvider, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<QueueManager>();
            _controllerProvider = controllerProvider;
        }

        public void SetupQueueHandle(IEnumerable<IClientProxy> clientProxies, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _clientProxies = clientProxies;
            foreach (var clientProxy in _clientProxies)
            {
                clientProxy.SetupQueueHandle(CreateQueueDictionary(), cancellationToken);
                clientProxy.MessageFromQueueReceived += ClientProxyOnMessageFromQueueReceived;
            }
        }

        public void Dispose()
        {
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
                   if (messageDefinition.MethodInfo.ReturnType?.GetNestedType(nameof(IMQResponse))!= null)
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
