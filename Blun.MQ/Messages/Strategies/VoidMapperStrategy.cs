using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Blun.MQ.Messages.Strategies
{
    internal sealed class VoidReturnTypeMapperStrategy : IReturnTypeMapperStrategy
    {
        public bool IsMappable(MethodInfo methodInfo)
        {
            var boolVoid = methodInfo.ReturnType == typeof(void);
            var boolTask = methodInfo.ReturnType == typeof(Task);

            return boolVoid || boolTask;
        }

        public IMQResponse ConvertInstance(object returnValue)
        {
            return null;
        }
    }
}
