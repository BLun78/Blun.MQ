using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Blun.MQ.Messages;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Controllers
{
    public interface IError
    {
        IMQResponse Error([NotNull] string result,[Optional, CanBeNull] Exception exception);

        IMQResponse Error([NotNull] object result,[Optional, CanBeNull]  Exception exception);
    }
}