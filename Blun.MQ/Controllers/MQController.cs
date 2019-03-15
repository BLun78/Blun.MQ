using System;
using System.Collections.Generic;
using System.Text;
using Blun.MQ.Message;

// ReSharper disable once CheckNamespace
namespace Blun.MQ
{
    // ReSharper disable once InconsistentNaming
    public abstract class MQController
    {
        // ReSharper disable once InconsistentNaming
        internal MQContext MQContext;

        protected MQContext Context => MQContext;

        protected MQController()
        {

        }
                
    }
}
