// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    internal interface IMessageResponseInfo: IMessageDefinition
    {
    string ReplayTo { get; }
    }
}