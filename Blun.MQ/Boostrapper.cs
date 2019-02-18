using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Blun.MQ
{
    internal class Boostrapper
    {
        private readonly IServiceCollection _serviceCollection;
       


        public Boostrapper()
        {
            
        }

        public Boostrapper(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }
    }
}
