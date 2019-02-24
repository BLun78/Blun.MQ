using System;
using Blun.MQ.Messages;

// ReSharper disable CheckNamespace

namespace Blun.MQ
{
    public class ReceiveMessageFromQueueEventArgs : EventArgs, IMessageDefinition
    {
        public string QueueName { get; set; }

        public string MessageName { get; set; }

        public string Key => $"{this.QueueName}.{this.MessageName}";

        public Message  Message { get; set; }


    }
}