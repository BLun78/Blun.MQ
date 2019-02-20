using System;
using System.Collections.Generic;
using System.Text;

namespace Blun.MQ.Abstractions
{
    public interface IMessageDefinition
    {
        string Key { get; }
        string QueueName { get; }
        string MessagePatternName { get; }
    }
}
