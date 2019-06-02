using System;
using System.Threading.Tasks;
using Blun.MQ.Messages;

// ReSharper disable once CheckNamespace
namespace Blun.MQ
{
    internal interface IProducer
    {
        Task<IMQResponse> SendAsync(IMQRequest request);
    }
}
