using Blun.MQ.Messages;

namespace Blun.MQ.Context
{
    // ReSharper disable once InconsistentNaming
    public class MQContext
    {
        public MQRequest Request { get; internal set; }
        public MQResponse Response { get; internal set; }
    }
}
