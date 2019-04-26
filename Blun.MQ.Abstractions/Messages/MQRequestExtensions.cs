using System;
using System.Net;
using System.Reflection;

// ReSharper disable CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    internal static class MQRequestExtensions
    {
        

        public static Message CreateMessage(this MQRequest request)
        {
            return request.CreateMessage(MessageType.Message);
        }

        public static Message CreateNullMessage(this MQRequest request)
        {
            return request.CreateMessage(MessageType.NullMessage);
        }

        private enum MessageType
        {
            Message,
            NullMessage
        }

        private static Message CreateMessage(this MQRequest request, MessageType messageType)
        {
            Message message = null;
            switch (messageType)
            {
                case MessageType.Message:
                    message = new Message(request.Message.MessageId, request.Message.Headers);
                    break;
                case MessageType.NullMessage:
                    message = new NullMessage(request.Message.MessageId, request.Message.Headers);
                    break;
            }
            return message;
        }
    }
}