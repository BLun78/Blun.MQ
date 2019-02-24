using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Abstractions;

namespace Blun.MQ.Hosting
{
    internal sealed partial class QueueManager : IDisposable
    {
        private readonly ControllerProvider _controllerProvider;
        private readonly IEnumerable<IClientProxy> _clientProxies;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public QueueManager(ControllerProvider controllerProvider, IEnumerable<IClientProxy> clientProxies)
        {
            _controllerProvider = controllerProvider;
            _clientProxies = clientProxies;
            _cancellationTokenSource = new CancellationTokenSource();
            SetupQueueHandle(_clientProxies, _cancellationTokenSource.Token);
        }

        private void SetupQueueHandle(IEnumerable<IClientProxy> clientProxies, CancellationToken cancellationToken)
        {
            foreach (var clientProxy in clientProxies)
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
            _cancellationTokenSource?.Cancel();
            foreach (var clientProxy in _clientProxies)
            {
                clientProxy?.Dispose();
            }
            _cancellationTokenSource?.Dispose();
        }
    }
}
