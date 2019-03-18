using System;
using System.Reflection;
// ReSharper disable CheckNamespace

namespace Blun.MQ.Messages
{
    internal class MessageDefinition : IMessageDefinition, IMessageDefinitionAssemblyInfos
    {
        public string Key => $"{this.QueueName}.{this.MessageName}";
        public string QueueName => this.QueueRouting.QueueName;
        public string MessageName => this.MessageRoute.MessagePattern;
        public string ReplayTo { get; }

        public QueueRoutingAttribute QueueRouting { get; internal set; }
        public Type ControllerType { get; internal set; }
        public MessageRouteAttribute MessageRoute { get; internal set; }
        public MethodInfo MethodInfo { get; internal set; }
    }
}