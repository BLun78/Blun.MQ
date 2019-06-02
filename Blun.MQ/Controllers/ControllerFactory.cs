﻿using System;
using Blun.MQ.Messages;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Blun.MQ.Controllers
{
    internal sealed class ControllerFactory
    {
        [CanBeNull]
        internal MQController CreateController([NotNull]IServiceScope serviceScope, [NotNull] Type type, [NotNull] MQContext mqContext)
        {
            var serviceProvider = serviceScope.ServiceProvider;
            if (!(serviceProvider.GetService(type) is MQController controller)) return null;
            controller.MQContext = mqContext;
            return controller;
        }
    }
}