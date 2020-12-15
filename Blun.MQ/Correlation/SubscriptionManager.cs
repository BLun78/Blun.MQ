using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Blun.MQ.Messages;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.Correlation
{
    internal class SubscriptionManager : ISubscriptionManager
    {
        private readonly ILogger<SubscriptionManager> _logger;
        private readonly IDictionary<string, ISubscription> _subscriptions;

        public SubscriptionManager(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SubscriptionManager>();
            _subscriptions = new ConcurrentDictionary<string, ISubscription>();
        }

        public string ResponseQueue { get; private set; } = "";

        public void Complete(string correlationId, IMQContext responseContext)
        {
            if (TryGetSubscription(correlationId, out var subscription))
            {
                subscription.Complete(responseContext);
            }
            else
            {
                // No Subscriber found: discard message and log
                _logger.LogWarning($"[SubscriptionManager] No Subscription found for id: {correlationId}");
            }
        }

        public bool HasActiveListener()
        {
            return _subscriptions.Any();
        }

        public void RegisterResponseQueue(string queueName)
        {
            _logger.LogInformation($"Response Queue '{queueName}' registered");
            ResponseQueue = queueName;
        }

        public ISubscription Subscribe(string correlationId, IMQContext context)
        {
            if (!_subscriptions.ContainsKey(correlationId))
            {
                lock (_subscriptions)
                {
                    if (!_subscriptions.ContainsKey(correlationId))
                    {
                        var sub = new Subscription(this, correlationId, context);
                        _subscriptions.Add(correlationId, sub);
                        return sub;
                    }

                }
            }
            throw new ArgumentException("Subscription with correlationId already exists", nameof(correlationId));
        }

        public bool TryGetSubscription(string correlationId, out ISubscription subscription)
        {
            return _subscriptions.TryGetValue(correlationId, out subscription);
        }

        public void Unsubscribe(string correlationId)
        {
            _logger.LogDebug($"[SubscriptionManager] Unsubscribe called for id: {correlationId}");
            if (_subscriptions.TryGetValue(correlationId, out var subscription))
            {
                _subscriptions.Remove(correlationId);
                subscription?.Dispose();
                // Log: Successfully Removed
            }
        }
        public void Unsubscribe(ISubscription subscription)
        {
            _logger.LogDebug($"[SubscriptionManager] Unsubscribe called for subscription: {subscription}");
            _subscriptions.Remove(subscription.CorrelationId);
            subscription?.Dispose();
        }
    }
}