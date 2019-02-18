using System;
using System.Collections.Generic;
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
        
    }
}
