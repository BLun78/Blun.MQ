﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Blun.MQ.Hosting
{
    internal sealed partial class QueueManager
    {
        internal static IDictionary<string, MessageDefinition> FindControllerByKey { get; private set; } = LoadControllers();
        internal static List<MessageDefinition> MessageDefinitions { get; private set; } = new List<MessageDefinition>();
        
        private static IDictionary<string, MessageDefinition> LoadControllers()
        {
            var controllers = new Dictionary<string, MessageDefinition>();
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

        private static void AddController(Type iMqController, IDictionary<string, MessageDefinition> controllers)
        {
            var messageDefinitions = LoadMqDefinition(iMqController);
            foreach (var definition in messageDefinitions)
            {
                    controllers.Add(definition.Key, definition);
            }
        }

        private static IEnumerable<MessageDefinition> LoadMqDefinition(Type iMqController)
        {
            var queues = LoadQueueAttribute(iMqController);
            return LoadMessagAttributes(iMqController, queues);
        }

        private static IEnumerable<MessageDefinition> LoadMessagAttributes(Type iMqController, IEnumerable<QueueAttribute> queueAttributes)
        {
            foreach (var methodInfo in iMqController.GetMethods())
            {
                foreach (var attribute in methodInfo.GetCustomAttributes(false))
                {
                    if (!(attribute is MessagePatternAttribute message)) continue;

                    foreach (var queueAttribute in queueAttributes)
                    {
                        yield return new MessageDefinition()
                        {
                            MethodInfo = methodInfo,
                            Queue = queueAttribute,
                            MessagePattern = message,
                            ControllerType = iMqController
                        };
                    }
                }
            }
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
    }
}