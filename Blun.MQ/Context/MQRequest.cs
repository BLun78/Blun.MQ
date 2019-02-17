using Blun.MQ.Messages;

namespace Blun.MQ.Context
{
    public class MQRequest {

        public Message Message { get; internal set; }

        public string Queue { get; internal set; }

        public string MessagePattern { get; internal set; }

    }
}
