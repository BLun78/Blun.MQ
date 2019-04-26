

// ReSharper disable CheckNamespace
namespace Blun.MQ.Messages
{
    internal interface IMessageDefinition
    {
        string Key { get; }
        string QueueName { get; }
        string MessageName { get; }
    }
}
