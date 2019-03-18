using System.Reflection;

namespace Blun.MQ.Messages.Strategies
{
    internal sealed class ValueTypeMapperStrategy : IReturnTypeMapperStrategy, IParameterMapperStrategy
    {
        public bool IsMappable(MethodInfo methodInfo)
        {
            return methodInfo.ReturnType.IsValueType;
        }

        public IMQResponse ConvertInstance(object returnValue)
        {
            throw new System.NotImplementedException();
        }

        public bool IsMappable(ParameterInfo parameterInfo)
        {
            return parameterInfo.ParameterType.IsValueType;
        }

        public object Mapping(int index, ParameterInfo parameterInfo, IMQRequest request)
        {
            throw new System.NotImplementedException();
        }
        
    }
}