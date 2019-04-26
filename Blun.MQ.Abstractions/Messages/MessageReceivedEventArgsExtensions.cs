// ReSharper disable once CheckNamespace

namespace Blun.MQ.Messages
{
    internal static class MessageReceivedEventArgsExtensions
    {
        // ReSharper disable once InconsistentNaming
        public static MQRequest CreateMQRequest(this MessageReceivedEventArgs eventArgs)
        {
            var request = new MQRequest
            {
                Message = eventArgs.Message,
                MessageRoute = eventArgs.MessageName
            };
            return request;
        }
    }
}