using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.RabbitMQ.Extensions
{
    // ReSharper disable once InconsistentNaming
    internal static class RabbitMQConsumer
    {
        private const string Prefix = "RabbitMQ";
        private const int StartLogEventId = 1000;
        
        public static void SetupQueueHandle(this ILogger<RabbitMQCosumer> logger, string queueName, [CallerMemberName] string method="")
        {
            logger.LogInformation(new EventId(StartLogEventId + 1, method), $"{Prefix}: Queue [{queueName}] is handled!" );
        }
    }
}