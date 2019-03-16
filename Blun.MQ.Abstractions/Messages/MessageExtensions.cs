using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

// ReSharper disable CheckNamespace
namespace Blun.MQ.Messages
{
    public static class MessageExtensions
    {
        public static IMQRequest CreateMQRequest(this Message message, string queueRoute, string messageRoute)
        {
            return new MQRequest()
            {
                Message = message,
                MessageRoute = messageRoute,
                QueueRoute = queueRoute,
                ContentLength = 0
            };
        }

        public static IMQResponse CreateMQResponse(this Message message, HttpStatusCode httpStatusCode)
        {
            return new MQResponse()
            {
                Message = message,
                HttpStatusCode = httpStatusCode,
                ContentLength = 0
            };
        }
    }
}
