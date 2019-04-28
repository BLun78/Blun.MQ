using System;
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
    public abstract partial class MQController : IOk
    {
        public IMQResponse Ok()
        {
            MQContext.MQResponse.MQStatusCode = MQStatusCode.Ok;
            MQContext.MQResponse.Message = MQContext.MQRequest.CreateNullMessage();
            MQContext.MQResponse.ContentLength = MQContext.MQResponse.Message.MessageSize;
            return MQContext.MQResponse;
        }

        public IMQResponse Ok([NotNull] string result)
        {
            if (string.IsNullOrWhiteSpace(result))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(result));
            MQContext.MQResponse.MQStatusCode = MQStatusCode.Ok;
            MQContext.MQResponse.Message = MQContext.MQRequest.CreateMessage(result);
            MQContext.MQResponse.ContentLength = MQContext.MQResponse.Message.MessageSize;
            return MQContext.MQResponse;
        }

        public IMQResponse Ok([NotNull] object result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            return OkImpl(result, result);
        }

        public IMQResponse Ok([NotNull] object result, [NotNull, ItemNotNull] params object[] results)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (results == null) throw new ArgumentNullException(nameof(results));
            return OkImpl(result, result);
        }

        private IMQResponse OkImpl([NotNull, ItemNotNull] params object[] results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            if (results.Length == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(results));

            try
            {
                var result = JsonConvert.SerializeObject(results);
                return Ok(result);
            }
            catch (JsonSerializationException jse)
            {
                _logger.LogJsonSerializationException(jse, "MQResult");
                return Error(jse.FlattenException(), jse);
            }
        }
    }
}