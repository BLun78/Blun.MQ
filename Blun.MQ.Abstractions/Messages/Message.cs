using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    public class Message
    {
        public Message()
        {
            Headers = new SortedDictionary<string, string>(StringComparer.InvariantCulture);
        }

        internal Message(string messageId) : this()
        {
            this.MessageId = messageId;
        }

        internal Message(string messageId, IDictionary<string, string> headers, string body)
        {
            this.MessageId = messageId;
            this.Headers = new SortedDictionary<string, string>(headers, StringComparer.InvariantCulture);
            this.Body = body;
        }

        public string MessageId { get; internal set; }

        public IDictionary<string, string> Headers { get; internal set; }

        public string Body { get; internal set; }

    }
}
