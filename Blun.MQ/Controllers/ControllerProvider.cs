using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Blun.MQ.Controllers
{
    internal sealed class ControllerProvider
    {
        private readonly ControllerFactory _controllerFactory;

        public IDictionary<string, Type> Controllers { get; private set; }

        internal ControllerProvider(ControllerFactory controllerFactory)
        {
            _controllerFactory = controllerFactory;
            this.Controllers = LoadControllers();
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

        private static IDictionary<string, Type> LoadControllers()
        {
            var controllers = new Dictionary<string, Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var types = assembly.GetTypes().Where(x =>
                    x.Assembly.FullName != Assembly.GetAssembly(typeof(IMQController)).FullName &&
                    x.Assembly.FullName != Assembly.GetAssembly(typeof(MQController)).FullName);

                foreach (var type in types)
                {
                    if (!type.GetInterfaces().Contains(typeof(IMQController))) continue;

                    AddController(type, controllers);
                }
            }

            return controllers;
        }

        private static void AddController(Type iMqController, IDictionary<string, Type> controllers)
        {
            var (queues, messages) = LoadMqAttributes(iMqController);
            foreach (var queue in queues)
            {
                foreach (var messageAttribute in messages)
                {
                    var key = $"{queue.QueueName}.{messageAttribute.MessageName}";
                    controllers.Add(key, iMqController);
                }
            }
        }

        private static (IEnumerable<QueueAttribute> queue, IEnumerable<MessageAttribute> messages) LoadMqAttributes(Type iMqController)
        {
            return (LoadQueueAttribute(iMqController), LoadMessageAttributes(iMqController));
        }

        private static IEnumerable<QueueAttribute> LoadQueueAttribute(MemberInfo iMqController)
        {
            IEnumerable<Attribute> attributes = iMqController.GetCustomAttributes().Where(x => x is QueueAttribute);
            foreach (var attribute in attributes)
            {
                if (attribute is QueueAttribute queue)
                {
                    yield return queue;
                }
            }
        }

        private static IEnumerable<MessageAttribute> LoadMessageAttributes(Type iMqController)
        {
            foreach (MethodInfo methodInfo in iMqController.GetMethods())
            {
                foreach (var attribute in methodInfo.GetCustomAttributes(false))
                {
                    if (attribute is MessageAttribute message)
                    {
                        yield return message;
                    }
                }
            }
        }
    }
}
