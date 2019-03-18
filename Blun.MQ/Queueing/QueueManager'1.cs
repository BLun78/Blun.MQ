using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blun.MQ.Exceptions;
using Blun.MQ.Messages;

namespace Blun.MQ.Queueing
{
    /// <summary>
    /// QueueManager creates the definitions for the queues
    /// </summary>
    internal sealed partial class QueueManager
    {

        /// <summary>
        /// Dictionary that contains the Controller for Key (QueueName + MethodName)
        /// </summary>
        internal static IDictionary<string, MessageDefinition> FindControllerByKey => _findControllerByKey.Value;

        /// <summary>
        /// Lazy backfield for <see cref="QueueManager.FindControllerByKey"/>
        /// </summary>
        private static Lazy<IDictionary<string, MessageDefinition>> _findControllerByKey { get; set; }

        /// <summary>
        /// Query over <see cref="QueueManager.FindControllerByKey"/> for MessageDefinitions
        /// </summary>
        internal static IQueryable<MessageDefinition> MessageDefinitions =>
            FindControllerByKey.Select(x => x.Value).AsQueryable();

        /// <summary>
        /// Static Ctor
        /// </summary>
        static QueueManager()
        {
            _findControllerByKey = new Lazy<IDictionary<string, MessageDefinition>>(LoadControllers);
        }

        /// <summary>
        /// Load all Controllers from appdomain.
        /// </summary>
        /// <returns>Dictionary with all Controllers and Queue definitions</returns>
        private static IDictionary<string, MessageDefinition> LoadControllers()
        {
            var controllers = new SortedDictionary<string, MessageDefinition>(StringComparer.InvariantCulture);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var types = assembly.GetTypes().Where(x =>
                    x.Assembly.FullName != Assembly.GetAssembly(typeof(MQController)).FullName);

                foreach (var type in types)
                {
                    if (!type.GetInterfaces().Contains(typeof(MQController))) continue;

                    AddController(type, controllers);
                }
            }
            return controllers;
        }

        private static void AddController(Type iMqController, IDictionary<string, MessageDefinition> controllers)
        {
            var messageDefinitions = LoadMqDefinition(iMqController);
            foreach (var definition in messageDefinitions)
            {
                try
                {
                    controllers.Add(definition.Key, definition);
                }
                catch (ArgumentException ae)
                {
                    throw new DuplicateKeyException($"The key [{definition.Key}] is duplicated in the FindControllerByKey dictionary!", ae);
                }
            }
        }

        private static IEnumerable<MessageDefinition> LoadMqDefinition(Type iMqController)
        {
            var queues = LoadQueueAttribute(iMqController);
            return LoadMessageAttributes(iMqController, queues);
        }

        private static IEnumerable<MessageDefinition> LoadMessageAttributes(Type iMqController, IEnumerable<QueueRoutingAttribute> queueAttributes)
        {
            IEnumerable<QueueRoutingAttribute> queueRoutingAttributes = queueAttributes as QueueRoutingAttribute[]
                                                                        ?? queueAttributes.ToArray();

            foreach (var methodInfo in iMqController.GetMethods())
            {
                foreach (var attribute in methodInfo.GetCustomAttributes(false))
                {
                    if (!(attribute is MessageRouteAttribute message)) continue;

                    foreach (var queueAttribute in queueRoutingAttributes)
                    {
                        yield return new MessageDefinition()
                        {
                            MethodInfo = methodInfo,
                            QueueRouting = queueAttribute,
                            MessageRoute = message,
                            ControllerType = iMqController
                        };
                    }
                }
            }
        }

        private static IEnumerable<QueueRoutingAttribute> LoadQueueAttribute(MemberInfo iMqController)
        {
            var attributes = iMqController.GetCustomAttributes().Where(x => x is QueueRoutingAttribute);
            foreach (var attribute in attributes)
            {
                if (attribute is QueueRoutingAttribute queue)
                {
                    yield return queue;
                }
            }
        }
    }
}