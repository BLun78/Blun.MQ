using System;
using System.Threading.Tasks;
using Blun.MQ.Controllers;
using Blun.MQ.Messages;

// ReSharper disable once CheckNamespace
namespace Blun.MQ
{
    // ReSharper disable once InconsistentNaming
    public abstract partial class MQController : INotOk
    {
        public IMQResponse NotOk()
        {
            throw new NotImplementedException();
        }

        public IMQResponse NotOk(string result)
        {
            throw new NotImplementedException();
        }

        public IMQResponse NotOk(string result, Exception exception)
        {
            throw new NotImplementedException();
        }

        public IMQResponse NotOk(object result)
        {
            throw new NotImplementedException();
        }

        public IMQResponse NotOk(object result, Exception exception)
        {
            throw new NotImplementedException();
        }
    }
}