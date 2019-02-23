using System;
using Blun.MQ.Messages;

// ReSharper disable CheckNamespace

namespace Blun.MQ
{
    public class ReceiveMessageFromQueueEventArgs : EventArgs
    {
        public string QueueName { get; set; }

        public Message  Message { get; set; }


    }
}