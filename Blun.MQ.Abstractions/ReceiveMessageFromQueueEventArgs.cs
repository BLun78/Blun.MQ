using System;

namespace Blun.MQ
{
    public class ReceiveMessageFromQueueEventArgs : EventArgs
    {
        public string QueueName { get; set; }
    }
}