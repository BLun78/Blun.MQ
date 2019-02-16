using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Blun.MQ
{
    public class Boostrapper
    {
        private readonly IServiceCollection _serviceCollection;
        private IClientProxy[] clientProxies;


        public Boostrapper()
        {
            
        }

        public Boostrapper(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }
    }
}
