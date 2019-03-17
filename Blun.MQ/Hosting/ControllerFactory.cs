using System;
using Blun.MQ.Messages;
using JetBrains.Annotations;

namespace Blun.MQ.Hosting
{
    internal sealed class ControllerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        internal ControllerFactory([NotNull] IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [CanBeNull]
        internal MQController GetController([NotNull] Type type, [NotNull] MQContext mqContext) 
        {
            if (!(this._serviceProvider.GetService(type) is MQController controller)) return null;
            controller.MQContext = mqContext;
            return controller;
        }
    }
}