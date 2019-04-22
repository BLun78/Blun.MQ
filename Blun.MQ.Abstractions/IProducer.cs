using System;
using System.Threading.Tasks;
using Blun.MQ.Messages;

namespace Blun.MQ.Abstractions
{
    public interface IProducer
    {
        Task<IMQResponse> SendAsync(IMQRequest request);
    }
}
