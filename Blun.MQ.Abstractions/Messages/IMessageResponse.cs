// ReSharper disable once CheckNamespace

namespace Blun.MQ.Messages
{
    internal interface IMessageResponseInfo
    {
        string ReplayTo { get; }
    }
}