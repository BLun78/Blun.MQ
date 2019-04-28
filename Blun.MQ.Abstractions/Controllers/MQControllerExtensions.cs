using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Controllers
{
    // ReSharper disable once InconsistentNaming
    internal static class MQControllerExtensions
    {
        private const int StartEventId = 2000;

        public static void LogJsonSerializationException(this ILogger<MQController> logger, JsonSerializationException jse, string where)
        {
            logger.LogWarning(new EventId(StartEventId, nameof(LogJsonSerializationException)), jse, $"The {where} for MQ mus be serializable!");
        }
    }
}