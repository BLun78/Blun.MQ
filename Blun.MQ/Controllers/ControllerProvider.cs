using System;
using System.Collections.Generic;
using System.Linq;
using Blun.MQ.Exceptions;
using Blun.MQ.Messages;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Blun.MQ.Controllers
{
    internal sealed class ControllerProvider
    {
        private readonly ControllerFactory _controllerFactory;
        private readonly MQContextFactory _mqContextFactory;

        [NotNull]
        public IDictionary<string, MessageDefinition> Controllers { get; } = Queueing.QueueManager.FindControllerByKey;

        public ControllerProvider(
            [NotNull] ControllerFactory controllerFactory,
            [NotNull] MQContextFactory mqContextFactory)
        {
            _controllerFactory = controllerFactory;
            _mqContextFactory = mqContextFactory;
        }

        /// <summary>
        /// Get the Controller for the messagequeue request
        /// </summary>
        /// <param name="serviceScope"></param>
        /// <param name="eventArgs"></param>
        /// <exception cref="KeyNotFoundException">The Key is not found in the dictonary</exception>
        /// <exception cref="ControllersAreEmptyException">The dictonary is empty</exception>
        /// <returns></returns>
        [CanBeNull]
        public MQController GetController(
            [NotNull] IServiceScope serviceScope,
            [NotNull] MessageReceivedEventArgs eventArgs)
        {
            var context = _mqContextFactory.CreateContext(serviceScope, eventArgs);
            var type = GetControllerType(eventArgs.Key);
            return this._controllerFactory.CreateController(serviceScope, type, context);
        }

        [NotNull]
        public Type GetControllerType([NotNull] string keyQueueMessage)
        {
            if (this.Controllers.ContainsKey(keyQueueMessage))
            {
                var messageDefinition = this.Controllers[keyQueueMessage];
                if (messageDefinition == null)
                {
                    throw new ControllerAreNullReferenceException($"Key [{keyQueueMessage}] return null!");
                }

                return this.Controllers[keyQueueMessage].ControllerType;
            }

            if (!this.Controllers.Any())
            {
                throw new ControllersAreEmptyException();
            }

            throw new KeyNotFoundException($"Key [{keyQueueMessage}] does not exists!");
        }
    }
}