using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Blun.MQ.Hosting
{
    internal class Host : IHostedService, IDisposable
    {
        private readonly IEnumerable<IClientProxy> _clientProxies;
        private readonly Queueing.QueueManager _queueManager;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public Host(
            IEnumerable<IClientProxy> allClientProxies,
            Queueing.QueueManager queueManager)
        {
            _clientProxies = allClientProxies;
            _queueManager = queueManager;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public  Task StartAsync(CancellationToken cancellationToken)
        {
            _queueManager.SetupQueueHandle(_clientProxies, _cancellationTokenSource.Token);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _queueManager.Dispose();
            foreach (var clientProxy in _clientProxies)
            {
                clientProxy?.Dispose();
            }
            _cancellationTokenSource.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
