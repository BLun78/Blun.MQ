﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Blun.MQ.Hosting
{
    internal sealed class ControllerProvider
    {
        private readonly ControllerFactory _controllerFactory;

        public IDictionary<string, MessageDefinition> Controllers => QueueManager.FindControllerByKey;

        internal ControllerProvider(ControllerFactory controllerFactory)
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
        internal IMQController GetController(string keyQueueMessage)
        {
            var type = GetControllerType(keyQueueMessage);
            return this._controllerFactory.GetController(type);
        }

        internal Type GetControllerType(string keyQueueMessage)
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