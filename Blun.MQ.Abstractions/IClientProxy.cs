using System;
using System.Threading.Tasks;

namespace Blun.MQ
{
    public interface IClientProxy : IDisposable
    {
        Task<string> SendAsync<T>(T message, string channel);

        void Connect();

        void Disconnect();

    }
}