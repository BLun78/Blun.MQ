using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Blun.MQ.Hosting
{
    internal sealed class QueueManager
    {
        public static IDictionary<string, Type> Controllers { get; private set; } = LoadControllers();
        public static List<string> Queues { get; private set; } = new List<string>();


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
                    var key = $"{queue.QueueName}.{messageAttribute.MessagePattern}";
                    Queues.Add(queue.QueueName);
                    controllers.Add(key, iMqController);
                }
            }
        }

        private static (IEnumerable<QueueAttribute> queue, IEnumerable<MessagePatternAttribute> messages) LoadMqAttributes(Type iMqController)
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

        private static IEnumerable<MessagePatternAttribute> LoadMessageAttributes(Type iMqController)
        {
            foreach (MethodInfo methodInfo in iMqController.GetMethods())
            {
                foreach (var attribute in methodInfo.GetCustomAttributes(false))
                {
                    if (attribute is MessagePatternAttribute message)
                    {
                        yield return message;
                    }
                }
            }
        }
    }
}