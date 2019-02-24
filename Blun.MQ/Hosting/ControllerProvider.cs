using System;
using System.Collections.Generic;
using System.Linq;
using Blun.MQ.Context;
using Blun.MQ.Exceptions;
using JetBrains.Annotations;

namespace Blun.MQ.Hosting
{
    internal sealed class ControllerProvider
    {
        private readonly ControllerFactory _controllerFactory;

        public IDictionary<string, MessageDefinition> Controllers { get; } = QueueManager.FindControllerByKey;

        internal ControllerProvider([NotNull] ControllerFactory controllerFactory)
        {
            _controllerFactory = controllerFactory;
        }

        /// <summary>
        /// Get the Controller for the messagequeue request
        /// </summary>
        /// <param name="keyQueueMessage"></param>
        /// <exception cref="KeyNotFoundException">The Key is not found in the dictonary</exception>
        /// <exception cref="ControllerAreEmptyException">The dictonary is empty</exception>
        /// <returns></returns>
        internal MQController GetController([NotNull] string keyQueueMessage, [NotNull] MQContext mqContext)
        {
            var type = GetControllerType(keyQueueMessage);
            return this._controllerFactory.GetController(type, mqContext);
        }

        internal Type GetControllerType([NotNull] string keyQueueMessage)
        {
            if (this.Controllers.ContainsKey(keyQueueMessage))
            {
                var messageDefinition = this.Controllers[keyQueueMessage];
                if (messageDefinition == null)
                {
                    throw new NullReferenceException($"Key [{keyQueueMessage}] return null!");
                }
                return this.Controllers[keyQueueMessage].ControllerType;
            }
            if (!this.Controllers.Any())
            {
                throw new ControllerAreEmptyException();
            }
            throw new KeyNotFoundException($"Key [{keyQueueMessage}] does not exists!");
        }
    }
}
