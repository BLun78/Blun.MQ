using System;
using Blun.MQ.Messages;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Blun.MQ.Controllers
{
    internal sealed class ControllerFactory
    {
        [CanBeNull]
        public MQController CreateController(
            [NotNull] IServiceScope serviceScope,
            [NotNull] Type type,
            [NotNull] MQContext mqContext)
        {
            if (!(serviceScope.ServiceProvider.GetService(type) is MQController controller)) return null;
            controller.MQContext = mqContext;
            
            if (serviceScope.ServiceProvider.GetService<IMQContextAccessor>() is MQContextAccessor mqMessageContext)
            {
                mqMessageContext.MQContext = mqContext;
            }

            return controller;
        }
    }
}