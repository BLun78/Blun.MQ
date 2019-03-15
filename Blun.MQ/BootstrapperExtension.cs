using Blun.MQ.Hosting;
using Blun.MQ.Message;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using QueueManager = Blun.MQ.Queueing.QueueManager;

namespace Blun.MQ
{
    public static class BootstrapperExtension{

        public static void AddMQ(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ControllerFactory>();
            serviceCollection.AddSingleton<ControllerProvider>();
            serviceCollection.AddSingleton<QueueManager>();
            serviceCollection.AddSingleton<Host>();
            serviceCollection.AddSingleton<MQContextFactory>();
        }

        public static void UseMq(this IApplicationBuilder applicationBuilder)
        {

        }
    }
}