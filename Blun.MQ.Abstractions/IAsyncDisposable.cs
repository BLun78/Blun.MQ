using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Blun.MQ.Abstractions
{
    public interface IAsyncDisposable
    {
        Task DisposeAsync();
    }
}
