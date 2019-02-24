using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace Blun.MQ.Hosting
{
    internal class Host : IDisposable
    {
        private readonly IEnumerable<IClientProxy> _clientProxies;
        private readonly QueueManager _queueManager;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public Host(IEnumerable<IClientProxy> allClientProxies,
            QueueManager queueManager)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _clientProxies = allClientProxies;
            _queueManager = queueManager;
            _queueManager.SetupQueueHandle(_clientProxies, _cancellationTokenSource.Token);
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _queueManager.Dispose();
            foreach (var clientProxy in _clientProxies)
            {
                clientProxy?.Dispose();
            }
            _cancellationTokenSource.Dispose();
        }
    }
}
