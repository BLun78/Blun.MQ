namespace Blun.MQ.Context
{
    public class MQContext
    {
        public MQRequest Request { get; internal set; }
        public MQResponse Response { get; internal set; }
    }
}
