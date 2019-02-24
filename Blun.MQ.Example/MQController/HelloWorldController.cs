using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Blun.MQ.Context;
using Blun.MQ.Messages;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Example.MQControllers
{
    [Queue("HelloWorld")]
    public class HelloWorldController : MQController
    {
        [MessagePattern("HelloWorld1")]
        public Message HelloWorld1(Message message)
        {
            return null;
        }

        [MessagePattern("HelloWorld2")]
        public MQResponse HelloWorld2(MQRequest request)
        {
            return null;
        }
    }
}