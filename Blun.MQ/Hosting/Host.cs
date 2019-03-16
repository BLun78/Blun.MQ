using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;

namespace Blun.MQ.Hosting
{
    internal class Host : IHostedService, IDisposable
    {
        private readonly Queueing.QueueManager _queueManager;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public Host(Queueing.QueueManager queueManager)
        {
            _queueManager = queueManager;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public Task StartAsync([NotNull] CancellationToken cancellationToken)
        {
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));

            return cancellationToken.IsCancellationRequested
                ? Task.CompletedTask
                : _queueManager.SetupQueueHandle(_cancellationTokenSource.Token);
        }

        public Task StopAsync([NotNull] CancellationToken cancellationToken)
        {
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));

            if (!cancellationToken.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
            return Task.CompletedTask;
        }
        
        protected virtual void Dispose(bool disposing)
        {
            // ReleaseUnmanagedResources();
            if (disposing)
            {
                _cancellationTokenSource?.Cancel();
                _queueManager?.Dispose();
                _cancellationTokenSource?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Host()
        {
            Dispose(false);
        }
    }
}
