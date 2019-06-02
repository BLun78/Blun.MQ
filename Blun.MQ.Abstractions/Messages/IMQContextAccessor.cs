// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    public interface IMQContextAccessor
    {
        // ReSharper disable once InconsistentNaming
        IMQContext MQContext { get; }
    }
}