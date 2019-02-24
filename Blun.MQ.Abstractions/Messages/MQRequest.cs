using Blun.MQ.Messages;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    public class MQRequest {

        public Message Message { get; internal set; }

        public string Queue { get; internal set; }

        public string MessagePattern { get; internal set; }

    }
}
