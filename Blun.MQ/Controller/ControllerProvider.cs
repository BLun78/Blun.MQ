using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Blun.MQ.Controller
{
    internal sealed class ControllerProvider
    {
        public IDictionary<string, Type> Controllers { get; private set; }

        public ControllerProvider()
        {
            this.Controllers = LoadControllers();
        }

        public Type GetControllerType(string keyQueueMessage)
        {
            if (this.Controllers.ContainsKey(keyQueueMessage))
            {
                return this.Controllers[keyQueueMessage];
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

                    var (queues, messages) = LoadMqAttributes(type);
                    foreach (var queue in queues)
                    {
                        foreach (var messageAttribute in messages)
                        {
                            var key = $"{queue.QueueName}.{messageAttribute.MessageName}";
                            controllers.Add(key, type);
                        }
                    }
                }
            }

            return controllers;
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
