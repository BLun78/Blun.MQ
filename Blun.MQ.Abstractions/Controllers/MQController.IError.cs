using System;
using System.Threading.Tasks;
using Blun.MQ.Controllers;
using Blun.MQ.Messages;

// ReSharper disable once CheckNamespace
namespace Blun.MQ
{
    // ReSharper disable once InconsistentNaming
    public abstract partial class MQController : IError
    {
        public IMQResponse Error(string result, Exception exception)
        {
            throw new NotImplementedException();
        }

        public IMQResponse Error(object result, Exception exception)
        {
            throw new NotImplementedException();
        }
    }
}