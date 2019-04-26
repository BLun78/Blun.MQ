using System;

// ReSharper disable CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    internal sealed class MQContextFactory
    {
        public MQContext CreateContext(MessageReceivedEventArgs eventArgs)
        {
            var context = new MQContext();
            context.MQRequest = eventArgs.CreateMQRequest();
            return context;
        }
    }
}
