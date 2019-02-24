using System;
using System.Collections.Generic;
using System.Text;
// ReSharper disable CheckNamespace

namespace Blun.MQ
{
    public interface IMessageDefinition
    {
        string Key { get; }
        string QueueName { get; }
        string MessageName { get; }
    }
}
