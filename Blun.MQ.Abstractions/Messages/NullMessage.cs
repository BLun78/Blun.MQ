using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    public class NullMessage : Message
    {
        internal NullMessage(string messageId) : base(messageId) {}
        
        internal NullMessage(string messageId,  IDictionary<string, string> headers) : base (messageId, headers, string.Empty) {}
    }
}