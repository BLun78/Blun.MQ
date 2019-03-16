using Blun.MQ.Messages;

namespace Blun.MQ.Message
{
    // ReSharper disable once InconsistentNaming
    public class MQContext
    {
        public IMQRequest Request { get; internal set; }
        public IMQResponse Response { get; internal set; }
    }
}
