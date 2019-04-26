using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Blun.MQ.Messages;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Blun.MQ
{

    // ReSharper disable once InconsistentNaming
    public abstract partial class MQController : IDisposable
    {
        private readonly ILogger<MQController> _logger;

        protected MQController(ILogger<MQController> logger)
        {
            _logger = logger;
        }

        // ReSharper disable once InconsistentNaming
        internal MQContext MQContext;
        
        /// <summary>
        /// MessageQueue Context
        /// </summary>
        protected MQContext Context => MQContext;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}