using System;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Messages;

namespace Blun.MQ.Correlation
{
    internal class Subscription : ISubscription, IDisposable
    {
        private readonly TaskCompletionSource<IMQContext> _tcs;
        private CancellationTokenSource _ct;
        private readonly ISubscriptionManager _subscriptionManager;
        private readonly IMQContext _messageContext;
        public string CorrelationId { get; }

        public Subscription(ISubscriptionManager subscriptionManager, string correlationId, IMQContext messageContext)
        {
            _messageContext = messageContext;
            _subscriptionManager = subscriptionManager;
            CorrelationId = correlationId;
            _tcs = new TaskCompletionSource<IMQContext>();
        }

        public void Complete(IMQContext result)
        {
            _tcs.TrySetResult(SyncMessageContext(_messageContext as MQContext, result));
            _subscriptionManager.Unsubscribe(CorrelationId);
        }

        public void Cancel()
        {
            _tcs.TrySetCanceled();
            _subscriptionManager.Unsubscribe(CorrelationId);
        }
        public Task<IMQContext> ListenAsync()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(30);
            return ListenAsync(timeout);
        }
        public Task<IMQContext> ListenAsync(TimeSpan timeout)
        {
            _ct = new CancellationTokenSource(timeout);
            _ct.Token.Register(Cancel, useSynchronizationContext: false);

            return _tcs.Task;
        }

        private IMQContext SyncMessageContext(MQContext originalContext, IMQContext responseContext)
        {
            originalContext.Response = responseContext.Request.ConvertToResponse();
            return originalContext;
        }

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // Cancel the task in case someone is still awaiting it's completion
                    Cancel();

                    _ct?.Dispose();
                }


                _disposedValue = true;
            }
        }



        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}