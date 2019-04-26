using Newtonsoft.Json;

namespace Blun.MQ.Messages
{
    internal static class MessageExtensions
    {
        public static string GetJson(this Message message)
        {
            return JsonConvert.SerializeObject(message);
        }

        public static int GetJsonSize(this Message message)
        {
            return message.GetJson().Length;
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