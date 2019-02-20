using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Blun.MQ.Abstractions;
namespace Blun.MQ.Hosting
{
    internal class Host : IDisposable
    {
        private readonly IEnumerable<IClientProxy> _clientProxies;
        private readonly QueueManager _queueManager;

        public Host(IEnumerable<IClientProxy> allClientProxies,
            QueueManager queueManager)
        {
            _clientProxies = allClientProxies;
            _queueManager = queueManager;
            _queueManager.SetupQueueHandle(_clientProxies);
        }
        
        public void Dispose()
        {
            foreach (IClientProxy clientProxy in _clientProxies)
            {
                clientProxy.Dispose();
            }
        }
    }
}
