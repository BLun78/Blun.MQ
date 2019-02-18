using System;
using System.Reflection;

namespace Blun.MQ.Hosting
{
    internal class MessageDefinition
    {
        public string Key => $"{this.QueueName}.{this.MessagePatternName}";
        public string QueueName => this.Queue.QueueName;
        public QueueAttribute Queue { get; internal set; }
        public Type ControllerType { get; internal set; }
        public string MessagePatternName => this.MessagePattern.MessagePattern;
        public MessagePatternAttribute MessagePattern { get; internal set; }
        public MethodInfo MethodInfo { get; internal set; }
    }
}   