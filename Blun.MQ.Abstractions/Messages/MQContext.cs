using System;

// ReSharper disable CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    public sealed class MQContext : IMQContext
    {
        internal MQContext()
        {
        }

        internal MQContext(MQRequest request)
        {
            Request = request;
            Response = new MQResponse();
        }


        public IMQRequest Request { get; internal set; }
        public IMQResponse Response { get; internal set; }
    }
}
