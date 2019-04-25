using System;
using System.Threading.Tasks;
using Blun.MQ.Messages;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Controllers
{
    public interface IError
    {
        IMQResponse Error(string result, Exception exception);

        IMQResponse Error(object result, Exception exception);
    }
}