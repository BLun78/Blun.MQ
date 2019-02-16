using System;
using System.Data.SqlTypes;

namespace Blun.MQ
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class MessageAttribute :  Attribute
    {
        private readonly string _messageName;

        public MessageAttribute(string messageName)
        {
            _messageName = messageName;
        }
    }
}