using System;

namespace Blun.MQ
{
    public interface IClientProxy : IDisposable
    {
        void Connect();

        void Disconnect();

    }
}