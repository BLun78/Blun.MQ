using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
        public IMQResponse Error([NotNull] string result,[Optional, CanBeNull] Exception exception)
        {
            if (string.IsNullOrWhiteSpace(result))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(result));
            
            
            
            MQContext.MQResponse.MQStatusCode = MQStatusCode.Ok;
            MQContext.MQResponse.Message = MQContext.MQRequest.CreateMessage(result);
            MQContext.MQResponse.ContentLength = MQContext.MQResponse.Message.MessageSize;
            return MQContext.MQResponse;
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
            
            MQContext.MQResponse.MQStatusCode = MQStatusCode.Ok;
            MQContext.MQResponse.Message = MQContext.MQRequest.CreateMessage(stringResult);
            MQContext.MQResponse.ContentLength = MQContext.MQResponse.Message.MessageSize;
            return MQContext.MQResponse;
        }
    }
}