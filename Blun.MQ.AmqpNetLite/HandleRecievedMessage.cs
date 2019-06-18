using Blun.MQ.Messages;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.AmqpNetLite
{
    internal class HandleRecievedMessage
    {
        private ILogger<HandleRecievedMessage> _logger;

        public HandleRecievedMessage(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HandleRecievedMessage>();
        }
        
//        public MessageReceivedEventArgs CreateEvent(Amqp.Message message)
//        {
//            
//            var resultMessage = new Message();
//            resultMessage.Body = message.Body.ToString();
//            
//            message.Footer
//                
//            return new MessageReceivedEventArgs(resultMessage, );
//        }
    }
}