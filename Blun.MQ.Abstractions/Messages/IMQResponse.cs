using System.Net;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    public interface IMQResponse
    {
        Message Message { get; }
        // ReSharper disable once InconsistentNaming
        MQStatusCode MQStatusCode { get; }
        long ContentLength { get; }
    }
}