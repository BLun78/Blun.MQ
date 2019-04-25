using System;
using System.Threading.Tasks;
using Blun.MQ.Messages;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Controllers
{
    public interface INotOk
    {
        IMQResponse NotOk();

        IMQResponse NotOk(string result);
        
        IMQResponse NotOk(string result, Exception exception);

        IMQResponse NotOk(object result);
        
        IMQResponse NotOk(object result, Exception exception);
    }
}