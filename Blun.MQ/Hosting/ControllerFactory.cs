using System;

namespace Blun.MQ.Hosting
{
    internal sealed class ControllerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        internal ControllerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        internal IMQController GetController(Type type) 
        {
            return this._serviceProvider.GetService(type) as IMQController;
        }
    }
}