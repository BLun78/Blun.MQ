using System;

// ReSharper disable CheckNamespace

namespace Blun.MQ.Messages
{
    public class ReceiveMessageFromQueueEventArgs : EventArgs, IMessageDefinition
    {
        public string QueueName { get; internal set; }

        public string MessageName { get; internal set; }

        public string ReplayToQueueNameAndId { get; internal set; }

        public string Key => $"{this.QueueName}.{this.MessageName}";

        public Message  Message { get; internal set; }


    }
}