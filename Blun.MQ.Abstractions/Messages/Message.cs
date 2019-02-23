using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    public class Message
    {
        public string MessageId { get; internal set; }

        public IDictionary<string, string> Headers { get; internal set; }

        public string Body { get; internal set; }

        public Message()
        {
            Headers = new SortedDictionary<string, string>();
        }

        public Message(string messageId, IDictionary<string, string> headers, string body)
        {
            this.MessageId = messageId;
            this.Headers = new SortedDictionary<string, string>(headers);
            this.Body = body;
        }
    }
}
