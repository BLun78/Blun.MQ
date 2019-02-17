using System;
using System.Collections.Generic;
using System.Linq;

namespace Blun.MQ.Hosting
{
    internal sealed class ControllerProvider
    {
        private readonly ControllerFactory _controllerFactory;
        private readonly QueueManager _queueManager;

        public IDictionary<string, Type> Controllers { get; private set; }

        internal ControllerProvider(ControllerFactory controllerFactory, QueueManager queueManager)
        {
            _controllerFactory = controllerFactory;
            _queueManager = queueManager;
            this.Controllers = QueueManager.LoadControllers();
        }

        /// <summary>
        /// Get the Controller for the messagequeue request
        /// </summary>
        /// <param name="keyQueueMessage"></param>
        /// <exception cref="KeyNotFoundException">The Key is not found in the dictonary</exception>
        /// <exception cref="ControllerAreEmptyException">The dictonary is empty</exception>
        /// <returns></returns>
        internal IMQController GetController(string keyQueueMessage)
        {
            var type = GetControllerType(keyQueueMessage);
            return this._controllerFactory.GetController(type);
        }

        internal Type GetControllerType(string keyQueueMessage)
        {
            if (this.Controllers.ContainsKey(keyQueueMessage))
            {
                return this.Controllers[keyQueueMessage];
            }
            if (!this.Controllers.Any())
            {
                throw new ControllerAreEmptyException();
            }
            throw new KeyNotFoundException($"Key [{keyQueueMessage}] does not exists!");
        }
    }
}
