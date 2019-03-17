using System;
using System.Collections.Generic;
using System.Text;
using Blun.MQ.Messages;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Blun.MQ
{
    // ReSharper disable once InconsistentNaming
    public abstract class MQController
    {
        private readonly ILogger<MQController> _logger;

        // ReSharper disable once InconsistentNaming
        internal MQContext MQContext;

        protected MQContext Context => MQContext;

        protected MQController(ILogger<MQController> logger)
        {
            _logger = logger;
        }
                
    }
}
