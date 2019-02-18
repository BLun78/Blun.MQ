using System.Reflection;

namespace Blun.MQ.Hosting
{
    public class MessageDefinition
    {
        public string QueueName => this.Queue.QueueName;
        internal QueueAttribute Queue { get; set; }
        public string MessagePatternName => this.MessagePattern.MessagePattern;
        internal MessagePatternAttribute MessagePattern { get; set; }
        public MethodInfo MethodInfo { get; internal set; }
    }
}   