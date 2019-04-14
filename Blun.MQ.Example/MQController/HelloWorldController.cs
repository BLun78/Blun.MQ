using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Blun.MQ.Messages;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Example.MQControllers
{
    [QueueRouting("HelloWorld")]
    public class HelloWorldController : MQController
    {
        [UsedImplicitly]
        [MessageRoute("HelloWorld1")]
        public Message HelloWorld1(Message message)
        {
            return null;
        }

        [UsedImplicitly]
        [MessageRoute("HelloWorld2")]
        public IMQResponse HelloWorld2(IMQRequest request)
        {
            return null;
        }
    }
}