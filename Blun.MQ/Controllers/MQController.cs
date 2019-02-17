using Blun.MQ.Context;
using System;
using System.Collections.Generic;
using System.Text;

namespace Blun.MQ
{
    public abstract class MQController : IMQController
    {
        protected MQContext Context { get; }


    }
}
