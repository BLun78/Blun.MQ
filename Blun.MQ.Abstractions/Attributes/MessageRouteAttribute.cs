using System;
using System.Data.SqlTypes;

// ReSharper disable once CheckNamespace
namespace Blun.MQ
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class MessageRouteAttribute :  Attribute
    {
        public string MessagePattern { get; }

        public MessageRouteAttribute(string MessagePattern)
        {
            this.MessagePattern = MessagePattern;
        }
    }
}