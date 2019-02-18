using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Blun.MQ.Abstractions;
namespace Blun.MQ.Hosting
{
    internal class Host : IAsyncDisposable
    {
        private readonly IClientProxy[] _clientProxies;
        private readonly QueueManager _queueManager;

        public Host(IClientProxy[] allClientProxies,
            QueueManager queueManager)
        {
            this._clientProxies = allClientProxies;
            this._queueManager = queueManager;
        }
        

        public Task DisposeAsync()
        {
            throw new NotImplementedException();
        }
    }
}
