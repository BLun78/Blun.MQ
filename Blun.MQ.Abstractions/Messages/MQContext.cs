// ReSharper disable CheckNamespace

namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    public sealed class MQContext
    {
        internal MQContext()
        {
        }

        internal MQContext(MQRequest request)
        {
            MQRequest = request;
            MQResponse = new MQResponse();
        }

        internal MQResponse MQResponse;
        internal MQRequest MQRequest;
        
        public IMQRequest Request => MQRequest;
        public IMQResponse Response => MQResponse;
    }
}
