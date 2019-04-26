using System.Threading.Tasks;
using Blun.MQ.Controllers;
using Blun.MQ.Messages;

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

        public IMQResponse Ok(string result)
        {
            MQContext.MQResponse.MQStatusCode = MQStatusCode.Ok;
            MQContext.MQResponse.Message = MQContext.MQRequest.CreateMessage();
            MQContext.MQResponse.ContentLength = result.Length;
            return MQContext.MQResponse;
        }

        public IMQResponse Ok(object result)
        {
            return OkImpl(result, result);
        }

        public IMQResponse Ok(object result, params object[] results)
        {
            return OkImpl(result, result);
        }

        private IMQResponse OkImpl(params object[] results)
        {
            MQContext.MQResponse.MQStatusCode = MQStatusCode.Ok;

            return MQContext.MQResponse;
        }
    }
}