using System.Net;

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

        public static IMQResponse CreateMQResponse(this Message message, MQStatusCode mqStatusCode)
        {
            return new MQResponse()
            {
                Message = message,
                MQStatusCode = mqStatusCode,
                ContentLength = 0
            };
        }
    }
}
