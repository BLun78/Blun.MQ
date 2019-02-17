using Blun.MQ.Context;
using System;
using System.Collections.Generic;
using System.Text;

namespace Blun.MQ
{
    public abstract class MQController : IMQController
    {
        internal MQContext MQContext;

        protected MQContext Context => MQContext;

        protected MQController()
        {

        }
                
    }
}
