

// ReSharper disable CheckNamespace
namespace Blun.MQ.Messages
{
    public interface IMessageDefinition
    {
        string Key { get; }
        string QueueName { get; }
        string MessageName { get; }
        string ReplayTo { get; }
    }
}
