using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Blun.MQ.Messages.Strategies
{
    internal sealed class MQMessageMapperStrategy : IMessageMapperStrategy
    {
        public bool IsStrategyAllowed(MethodInfo methodInfo)
        {
            throw new NotImplementedException();
        }
    }
}
