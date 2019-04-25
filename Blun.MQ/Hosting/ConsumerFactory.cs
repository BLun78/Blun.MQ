using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Blun.MQ.Messages;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.Hosting
{
    internal sealed class ConsumerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ConsumerFactory> _logger;

        public ConsumerFactory(
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] IServiceProvider serviceProvider)
        {
            _logger = loggerFactory.CreateLogger<ConsumerFactory>();
            _serviceProvider = serviceProvider;
        }

        public ConsumerManager CreateSubscriber(KeyValuePair<string, IEnumerable<IMessageDefinition>> messageDefinition,
            CancellationToken cancellationToken)
        {
            var consumer = _serviceProvider.GetService<ConsumerManager>();

            consumer.SetupQueueHandle(messageDefinition, cancellationToken);

            return consumer;
        }
    }
}