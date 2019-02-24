using System;
using System.Data.SqlTypes;

// ReSharper disable once CheckNamespace
namespace Blun.MQ
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class MessagePatternAttribute :  Attribute
    {
        public string MessagePattern { get; }

        public MessagePatternAttribute(string MessagePattern)
        {
            this.MessagePattern = MessagePattern;
        }
    }
}