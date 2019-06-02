// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    public interface IMQContext
    {
        IMQRequest Request {get;}
        IMQResponse Response  {get;}
    }
}