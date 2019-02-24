using System;
using System.Collections.Generic;
using System.Text;
using Blun.MQ.Hosting;

namespace Blun.MQ.Context
{
    // ReSharper disable once InconsistentNaming
    internal sealed class MQContextFactory
    {
        public MQContext CreateContext(MessageDefinition messageDefinition)
        {
            var context = new MQContext();


            return context;
        }
    }
}
