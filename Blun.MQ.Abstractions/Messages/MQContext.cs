// ReSharper disable CheckNamespace

namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    public class MQContext
    {
        public MQContext()
        {
        }

        internal MQContext(IMQRequest request)
        {
            Request = request;
            Response = new MQResponse();
        }
        
        public IMQRequest Request { get; internal set; }
        public IMQResponse Response { get; internal set; }
    }
}
