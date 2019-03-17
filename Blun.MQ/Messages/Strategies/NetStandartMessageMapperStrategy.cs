using System;
using System.Reflection;

namespace Blun.MQ.Messages.Strategies
{
    internal sealed class NetStandartMessageMapperStrategy : IMessageMapperStrategy
    {
        public bool IsStrategyAllowed(MethodInfo methodInfo)
        {
            throw new NotImplementedException();
        }
    }
}