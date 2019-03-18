using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Blun.MQ.Messages.Strategies;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.Messages
{
    internal class MessageMapper
    {
        [NotNull] [ItemNotNull] private readonly IEnumerable<IParameterMapperStrategy> _parameterMapperStrategies;
        [NotNull] [ItemNotNull] private readonly IEnumerable<IReturnTypeMapperStrategy> _returnTypeMapperStrategies;
        [NotNull] private readonly ILogger<MessageMapper> _logger;

        public MessageMapper(
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull, ItemNotNull] IEnumerable<IMapperStrategy> mapperStrategies)
        {
            _logger = loggerFactory.CreateLogger<MessageMapper>();
            var strategies = mapperStrategies as IMapperStrategy[] ?? mapperStrategies.ToArray();
            _parameterMapperStrategies = strategies
                .Where(x => x.GetType().GetInterfaces().Contains(typeof(IParameterMapperStrategy)))
                .Cast<IParameterMapperStrategy>();
            _returnTypeMapperStrategies = strategies
                .Where(x => x.GetType().GetInterfaces().Contains(typeof(IReturnTypeMapperStrategy)))
                .Cast<IReturnTypeMapperStrategy>();
        }

        public object[] CreateParameters([NotNull] MessageDefinition messageDefinition)
        {
            var parameterInfos = messageDefinition.MethodInfo.GetParameters();

            foreach (ParameterInfo parameterInfo in parameterInfos)
            {
             
            }


            return null;
        }
    }
}
