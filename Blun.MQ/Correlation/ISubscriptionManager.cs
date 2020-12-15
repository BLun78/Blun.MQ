using Blun.MQ.Messages;

namespace Blun.MQ.Correlation
{
    internal interface ISubscriptionManager
    {
        string ResponseQueue { get; }
        ISubscription Subscribe(string correlationId, IMQContext context);
        void RegisterResponseQueue(string queueName);
        void Unsubscribe(string correlationId);
        void Unsubscribe(ISubscription subscription);
        void Complete(string correlationId, IMQContext context);
        bool HasActiveListener();
        bool TryGetSubscription(string correlationId, out ISubscription subscription);

    }
}