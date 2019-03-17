using System.Net;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    internal class MQResponse : IMQResponse
    {
        public Message Message { get; internal set; }
        public HttpStatusCode HttpStatusCode { get; set; }
        public long ContentLength { get; internal set; }
    }
}
