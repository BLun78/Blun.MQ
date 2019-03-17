

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    public interface IMQRequest
    {
        Messages.Message Message { get; }
        string QueueRoute { get; }
        string MessageRoute { get; }
        long ContentLength { get; }
    }
}