using System.Net;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    public interface IMQResponse
    {
        Message Message { get; }
        HttpStatusCode HttpStatusCode { get; set; }
        long ContentLength { get; }
    }
}