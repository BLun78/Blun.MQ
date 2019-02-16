using System;
using System.Collections.Generic;
using System.Text;

namespace Blun.MQ
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class QueueAttribute : Attribute
    {
        private readonly string _queueName;

        public QueueAttribute(string queueName)
        {
            _queueName = queueName;
        }
    }
}
