using System.Runtime.CompilerServices;
using Blun.MQ.Hosting;
using Blun.MQ.Message;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using QueueManager = Blun.MQ.Queueing.QueueManager;

namespace Blun.MQ
{
    // ReSharper disable once InconsistentNaming
    public static class ServiceCollectionMQExtension
    {
        /// <summary>
        /// Register thw Blun.MQ Framework
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to register with.</param>
        /// <returns>The original <see cref="IServiceCollection"/>.</returns>
        // ReSharper disable once InconsistentNaming
        public static IServiceCollection AddMQ(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddHostedService<Host>();
            serviceCollection.AddSingleton<QueueManager>();
            serviceCollection.AddSingleton<ControllerFactory>();
            serviceCollection.AddSingleton<ControllerProvider>();
            serviceCollection.AddSingleton<SubscriberFactory>();
            serviceCollection.AddSingleton<MQContextFactory>();

            return serviceCollection;
        }
    }
}