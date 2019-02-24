using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable CheckNamespace

namespace Blun.MQ.Abstractions
{
    public interface IAsyncDisposable
    {
        Task DisposeAsync([Optional] CancellationToken cancellationToken);
    }
}
