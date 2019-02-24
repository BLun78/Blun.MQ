using System;
using System.Collections.Generic;
using System.Text;
// ReSharper disable CheckNamespace

namespace Blun.MQ.Abstractions
{
    public interface IMessageDefinition
    {
        string Key { get; }
        string QueueName { get; }
        string MessageName { get; }
    }
}
