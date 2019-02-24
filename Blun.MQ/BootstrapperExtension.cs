using Blun.MQ.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Blun.MQ
{
    public static class BootstrapperExtension{

        public static void AddMQ(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ControllerFactory>();
            serviceCollection.AddSingleton<ControllerProvider>();
            serviceCollection.AddSingleton<QueueManager>();
            serviceCollection.AddSingleton<Host>();

        }

        public static void UseMq(this IApplicationBuilder applicationBuilder)
        {

        }
    }
}