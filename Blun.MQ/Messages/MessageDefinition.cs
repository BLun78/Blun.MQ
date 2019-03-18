using System;
using System.Reflection;
// ReSharper disable CheckNamespace

namespace Blun.MQ.Messages
{

    internal class MessageDefinition : IMessageDefinition
    {
        public string Key => $"{this.QueueName}.{this.MessageName}";
        public string QueueName => this.QueueRouting.QueueName;
        public QueueRoutingAttribute QueueRouting { get; internal set; }
        public Type ControllerType { get; internal set; }
        public string MessageName => this.MessageRoute.MessagePattern;
        public string ReplayTo { get; }
        public MessageRouteAttribute MessageRoute { get; internal set; }
        public MethodInfo MethodInfo { get; internal set; }
    }
}