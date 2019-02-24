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
                clientProxy.SetupQueueHandle(MessageDefinitions
                        .OrderBy(x => x.QueueName, StringComparer.Ordinal)
                        .Select(x => x.QueueName)
                        .Distinct(StringComparer.Ordinal),
                    cancellationToken);

            }
        }

        public void Dispose()
        {
        }
    }
}
