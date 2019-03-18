using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;

namespace Blun.MQ.Hosting
{
    /// <summary>
    /// Blun.MQ Host
    /// </summary>
    internal class Host : IHostedService, IDisposable
    {
        private readonly Queueing.QueueManager _queueManager;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public Host(Queueing.QueueManager queueManager)
        {
            _queueManager = queueManager;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public Task StartAsync([NotNull] CancellationToken cancellationToken)
        {
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));

            return cancellationToken.IsCancellationRequested
                ? Task.CompletedTask
                : _queueManager.SetupQueueHandle(_cancellationTokenSource.Token);
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
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
