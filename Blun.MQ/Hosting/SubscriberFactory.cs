﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.Hosting
{
    internal sealed class SubscriberFactory
    {
        private readonly IClientProxy _clientProxy;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SubscriberFactory> _logger;

        public SubscriberFactory(
            ILoggerFactory loggerFactory,
            IClientProxy clientProxy,
            IServiceProvider serviceProvider)
        {
            _logger = loggerFactory.CreateLogger<SubscriberFactory>();
            _clientProxy = clientProxy;
            _serviceProvider = serviceProvider;
        }

        public ISubscriber CreateSubscriber(KeyValuePair<string, IEnumerable<IMessageDefinition>> messageDefinition , CancellationToken cancellationToken)
        {
            var subscriber =  _serviceProvider.GetService<ISubscriber>();
            var dictionary = new Dictionary<string, IEnumerable<IMessageDefinition>>
            {
                {messageDefinition.Key, messageDefinition.Value}
            };
            subscriber.SetupQueueHandle(dictionary, cancellationToken);

            return subscriber;
        }
    }
}