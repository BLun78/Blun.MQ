using Blun.MQ.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Blun.MQ
{
    public static class BoostrapperExtension{

        public static void addMQ(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ControllerFactory>();
            serviceCollection.AddSingleton<ControllerProvider>();

        }
    }
}