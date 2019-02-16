using System;
using System.Collections.Generic;
using System.Text;

namespace Blun.MQ
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class QueueAttribute : Attribute
    {
        public string QueueName { get; }

        public QueueAttribute(string queueName)
        {
            QueueName = queueName;
        }
    }
}
