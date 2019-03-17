using Blun.MQ.Hosting;
using Blun.MQ.Messages;
using Blun.MQ.Messages.Strategies;
using Microsoft.Extensions.DependencyInjection;
using QueueManager = Blun.MQ.Queueing.QueueManager;

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
            serviceCollection.AddTransient<QueueManager>();
            serviceCollection.AddTransient<ControllerFactory>();
            serviceCollection.AddTransient<ControllerProvider>();
            serviceCollection.AddTransient<SubscriberFactory>();
            serviceCollection.AddTransient<MQContextFactory>();
            serviceCollection.AddTransient<MessageRouter>();
            serviceCollection.AddTransient<Subscriber>();
            serviceCollection.AddTransient<IMessageMapperStrategy, MQMessageMapperStrategy>();
            serviceCollection.AddTransient<IMessageMapperStrategy, NetStandartMessageMapperStrategy>();
            
            return serviceCollection;
        }
    }
}