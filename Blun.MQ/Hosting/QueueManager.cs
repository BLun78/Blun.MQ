using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Blun.MQ.Hosting
{
    internal sealed partial class QueueManager : IDisposable
    {
        private readonly ControllerProvider _controllerProvider;
        private IEnumerable<IClientProxy> _clientProxies;

        public QueueManager(ControllerProvider controllerProvider)
        {
            _controllerProvider = controllerProvider;
        }

        public void SetupQueueHandle(IEnumerable<IClientProxy> clientProxies, CancellationToken cancellationToken)
        {
            _clientProxies = clientProxies;
            foreach (var clientProxy in _clientProxies)
            {
                clientProxy.SetupQueueHandle(CreateQueueDictionary(), cancellationToken);
                clientProxy.MessageFromQueueReceived += ClientProxyOnMessageFromQueueReceived;
            }
        }

        private void ClientProxyOnMessageFromQueueReceived(object sender, ReceiveMessageFromQueueEventArgs e)
        {
            throw new NotImplementedException();
        }

        private IDictionary<string, IEnumerable<IMessageDefinition>> CreateQueueDictionary()
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

        public void Dispose()
        {
        }
    }
}
