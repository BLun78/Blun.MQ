using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Context;
using JetBrains.Annotations;

namespace Blun.MQ.Hosting
{
    internal sealed partial class QueueManager : IDisposable
    {
        private readonly ControllerProvider _controllerProvider;
        private IEnumerable<IClientProxy> _clientProxies;
        private CancellationToken _cancellationToken;

        public QueueManager(ControllerProvider controllerProvider)
        {
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
            _ = Task.Run( () =>
            {
                var controller = _controllerProvider.GetController(e);

            }, _cancellationToken);
        }

        private static IDictionary<string, IEnumerable<IMessageDefinition>> CreateQueueDictionary()
        {
            var result = new SortedDictionary<string, IEnumerable<IMessageDefinition>>();
            var queueNames = MessageDefinitions
                .OrderBy(o => o.QueueName, StringComparer.Ordinal)
                .Select(s => s.QueueName)
                .Distinct(StringComparer.Ordinal);

            foreach (var queueName in queueNames)
            {
                var messageDefinitions = MessageDefinitions
                    .Where(w => w.QueueName.Equals(queueName, StringComparison.Ordinal))
                    .OrderBy(o => o.QueueName, StringComparer.Ordinal);
                result.Add(queueName, messageDefinitions);
            }

            return result;
        }

    }
}
