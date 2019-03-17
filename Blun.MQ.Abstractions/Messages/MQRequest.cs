

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    internal class MQRequest : IMQRequest
    {

        public Message Message { get; internal set; }

        public string QueueRoute { get; internal set; }

        public string MessageRoute { get; internal set; }

        public long ContentLength { get; internal set; }
    }
}
