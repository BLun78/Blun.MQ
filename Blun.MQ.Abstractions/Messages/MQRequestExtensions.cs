using System;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

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

        public static Message CreateMessage(this MQRequest request, string body)
        {
            return request.CreateMessage(MessageType.Message, body);
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

        private static Message CreateMessage(this MQRequest request, MessageType messageType, [Optional] string body)
        {
            Message message = null;
            switch (messageType)
            {
                case MessageType.Message:
                    if (string.IsNullOrWhiteSpace(body))
                    {
                        message = new Message(request.Message.MessageId, request.Message.Headers);
                    }
                    else
                    {
                        message = new Message(request.Message.MessageId, request.Message.Headers, body);
                    }

                    break;
                case MessageType.NullMessage:
                    if (!string.IsNullOrWhiteSpace(body)) throw new ArgumentException("The Enum:MessageType.NullMessage match not with '!string.IsNullOrWhiteSpace(body)'! A NullMessage needs no body!");
                    message = new NullMessage(request.Message.MessageId, request.Message.Headers);
                    break;
            }
            return message;
        }
    }
}