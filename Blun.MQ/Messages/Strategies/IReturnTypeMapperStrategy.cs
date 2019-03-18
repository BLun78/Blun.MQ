using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Blun.MQ.Messages.Strategies
{
    internal interface IReturnTypeMapperStrategy : IMapperStrategy
    {
        IMQResponse ConvertInstance(object returnValue);
    }
}
