using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Blun.MQ
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class QueueRoutingAttribute : Attribute
    {
        public string QueueName { get; }

        public QueueRoutingAttribute(string queueName)
        {
            QueueName = queueName;
        }
    }
}
