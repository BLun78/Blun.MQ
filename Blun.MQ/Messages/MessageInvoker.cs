using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Controllers;
using Blun.MQ.Hosting;
using Blun.MQ.Queueing;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable CheckNamespace
namespace Blun.MQ.Messages
{
    internal sealed class MessageInvoker
    {
        [NotNull] private readonly ILogger<MessageInvoker> _logger;
        [NotNull] private readonly ControllerProvider _controllerProvider;
        [NotNull] private readonly MessageMapper _messageMapper;
        [NotNull] private readonly IServiceProvider _serviceProvider;

        public MessageInvoker(
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] IServiceProvider serviceProvider,
            [NotNull] ControllerProvider controllerProvider,
            [NotNull] MessageMapper messageMapper)
        {
            _logger = loggerFactory.CreateLogger<MessageInvoker>();
            _serviceProvider = serviceProvider;
            _controllerProvider = controllerProvider;
            _messageMapper = messageMapper;
        }

        public Task HandleMessage(
            [NotNull] MessageReceivedEventArgs eventArgs,
            [NotNull] CancellationToken cancellationToken)
        {
            if (cancellationToken == null) throw new InvalidOperationException("SetupQueueHandle() wasn't run!");

            return Task.Run(() => HandleScopedMessage(eventArgs), cancellationToken);
        }

        private void HandleScopedMessage([NotNull] MessageReceivedEventArgs eventArgs)
        {
            using (_logger.BeginScope("ReceiveMessageFromQueue"))
            using (var serviceScope = _serviceProvider.CreateScope())
            {
                var messageDefinition = QueueManager.FindControllerByKey[eventArgs.Key];

                var controller = _controllerProvider.GetController(serviceScope, eventArgs);
                var parameters = _messageMapper.CreateParameters(messageDefinition);
                
            }
        }
    }
}