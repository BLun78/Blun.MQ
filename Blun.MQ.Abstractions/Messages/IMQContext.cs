// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    public interface IMQContext
    {
        IMQRequest Request {get;}
        IMQResponse Response  {get;}
    }
}