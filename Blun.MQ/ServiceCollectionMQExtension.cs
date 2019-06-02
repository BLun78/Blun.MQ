using Blun.MQ.Controllers;
using Blun.MQ.Hosting;
using Blun.MQ.Messages;
using Blun.MQ.Messages.Strategies;
using Blun.MQ.Queueing;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable CheckNamespace
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
            serviceCollection.AddSingleton<ConsumerFactory>();
            serviceCollection.AddSingleton<MQContextFactory>();
            serviceCollection.AddTransient<MessageInvoker>();
            serviceCollection.AddSingleton<IMQContextAccessor, MQContextAccessor>();
            serviceCollection.AddTransient<IMapperStrategy, VoidReturnTypeMapperStrategy>();
            serviceCollection.AddTransient<IMapperStrategy, ValueTypeMapperStrategy>();

            return serviceCollection;
        }
    }
}