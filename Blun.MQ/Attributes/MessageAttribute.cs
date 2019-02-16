using System;
using System.Data.SqlTypes;

namespace Blun.MQ
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class MessageAttribute :  Attribute
    {
        public string MessageName { get; }

        public MessageAttribute(string messageName)
        {
            MessageName = messageName;
        }
    }
}