using System;
using System.Threading.Tasks;
using Blun.MQ.Messages;

namespace Blun.MQ.Correlation
{
    internal interface ISubscription : IDisposable
    {
        string CorrelationId { get; }
        Task<IMQContext> ListenAsync();
        Task<IMQContext> ListenAsync(TimeSpan timeout);
        void Complete(IMQContext message);
    }
}
