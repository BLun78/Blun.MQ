using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace Blun.MQ.Controller
{
    internal sealed class ControllerProvider
    {
        public IDictionary<string, Type> Controllers { get; }

        public ControllerProvider()
        {
            this.Controllers = new Dictionary<string, Type>();
        }

        public void LoadControllers()
        {
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
                            this.Controllers.Add(key, type);
                        }
                    }
                    
                }
            }
        }

        private (IEnumerable<QueueAttribute> queue, IEnumerable<MessageAttribute> messages) LoadMqAttributes(Type iMqController)
        {
            return (LoadQueueAttribute(iMqController), LoadMessageAttributes(iMqController));
        }

        private IEnumerable<QueueAttribute> LoadQueueAttribute(MemberInfo iMqController)
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
        
        private IEnumerable<MessageAttribute> LoadMessageAttributes(Type iMqController)
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
