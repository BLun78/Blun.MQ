using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Blun.MQ.Hosting
{
    internal sealed partial class QueueManager
    {
        private readonly ControllerProvider _controllerProvider;

        public QueueManager(ControllerProvider controllerProvider)
        {
            _controllerProvider = controllerProvider;
        }

        public void SetupQueueHandle(IEnumerable<IClientProxy> clientProxies)
        {
            foreach (var clientProxy in clientProxies)
            {
                clientProxy.SetupQueueHandle(MessageDefinitions.Select(x => x.QueueName));
            }
        }
    }
}
