using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Blun.MQ.Common;
using Blun.MQ.Controllers;
using Blun.MQ.Messages;
using JetBrains.Annotations;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace Blun.MQ
{
    // ReSharper disable once InconsistentNaming
    public abstract partial class MQController : IError
    {
        public IMQResponse Error([NotNull] string message, [Optional, CanBeNull] Exception exception)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(message));

            var result = $"{message}{Environment.NewLine}{exception.FlattenException()}";

            return CreateMQResponse(result, MQStatusCode.Error);
        }

        public IMQResponse Error([NotNull] object message, [Optional, CanBeNull] Exception exception)

        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var stringResult = string.Empty;
            try
            {
                stringResult = JsonConvert.SerializeObject(message);
            }
            catch (JsonSerializationException jse)
            {
                _logger.LogJsonSerializationException(jse, "Error Message");
            }

            var result = string.IsNullOrWhiteSpace(stringResult)
                ? $"The error message is not serializable!{Environment.NewLine}{exception.FlattenException()}"
                : $"{stringResult}{Environment.NewLine}{exception.FlattenException()}";

            return CreateMQResponse(result, MQStatusCode.Error);
        }
    }
}