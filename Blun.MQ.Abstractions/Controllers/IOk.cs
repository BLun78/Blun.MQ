using System.Threading.Tasks;
using Blun.MQ.Messages;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Controllers
{
    public interface IOk
    {
        IMQResponse Ok();

        IMQResponse Ok(string result);

        IMQResponse Ok(object result);

        IMQResponse Ok(object result, params object[] results);
    }
}