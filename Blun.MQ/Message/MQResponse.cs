using Blun.MQ.Messages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    public class MQResponse : IMQResponse
    {
        public Message Message { get; internal set; }
        public HttpStatusCode HttpStatusCode { get; set; }
        public long ContentLength { get; internal set; }
    }
}
