using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Blun.MQ.Exceptions;

namespace Blun.MQ.Hosting
{
    internal sealed partial class QueueManager
    {
        internal static IDictionary<string, MessageDefinition> FindControllerByKey { get; private set; } = LoadControllers();
        internal static List<MessageDefinition> MessageDefinitions { get; private set; } = new List<MessageDefinition>();

        private static IDictionary<string, MessageDefinition> LoadControllers()
        {
            var controllers = new SortedDictionary<string, MessageDefinition>(StringComparer.Ordinal);
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
            foreach (var methodInfo in iMqController.GetMethods())
            {
                foreach (var attribute in methodInfo.GetCustomAttributes(false))
                {
                    if (!(attribute is MessageRouteAttribute message)) continue;

                    foreach (var queueAttribute in queueAttributes)
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