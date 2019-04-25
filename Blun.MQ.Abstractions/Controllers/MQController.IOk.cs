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
            throw new System.NotImplementedException();
        }

        public IMQResponse Ok(string result)
        {
            throw new System.NotImplementedException();
        }
        
        public IMQResponse Ok(object result)
        {
            throw new System.NotImplementedException();
        }

        public IMQResponse Ok(object result, params object[] results)
        {
            throw new System.NotImplementedException();
        }
    }
}