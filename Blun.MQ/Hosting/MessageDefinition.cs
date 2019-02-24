using System;
using System.Reflection;

namespace Blun.MQ.Hosting
{

    internal class MessageDefinition : IMessageDefinition
    {
        public string Key => $"{this.QueueName}.{this.MessageName}";
        public string QueueName => this.QueueRouting.QueueName;
        public QueueRoutingAttribute QueueRouting { get; internal set; }
        public Type ControllerType { get; internal set; }
        public string MessageName => this.MessageRoute.MessagePattern;
        public MessageRouteAttribute MessageRoute { get; internal set; }
        public MethodInfo MethodInfo { get; internal set; }
    }
}   