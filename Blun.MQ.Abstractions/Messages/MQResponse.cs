using Blun.MQ.Messages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Blun.MQ.Context
{

    public class MQResponse
    {
        public Message Message { get; internal set; }
        
        public HttpStatusCode HttpStatusCode { get; set; }

        public long ContentLength { get; internal set; }
    }
}
