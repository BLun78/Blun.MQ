﻿using System;

// ReSharper disable CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    internal sealed class MQContextFactory
    {
        public MQContext CreateContext(IMessageDefinition messageDefinition, IMQRequest request)
        {
            var context = new MQContext();
        

            return context;
        }
    }
}
