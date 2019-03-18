using System.Reflection;

namespace Blun.MQ.Messages.Strategies
{
    internal interface IParameterMapperStrategy : IMapperStrategy
    {
        bool IsMappable(ParameterInfo parameterInfo);

         object Mapping(int index, ParameterInfo parameterInfo, IMQRequest request);
    }
}