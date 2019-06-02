using Microsoft.Extensions.DependencyInjection;
using Blun.MQ;

// ReSharper disable CheckNamespace
namespace Blun.MQ.RabbitMQ
{
    // ReSharper disable once InconsistentNaming
    public static class ServiceCollectionMQExtension
    {
        /// <summary>
        /// Register thw Blun.MQ.RabbitMQ Framework
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/> to register with.</param>
        /// <returns>The original <see cref="IServiceCollection"/>.</returns>
        // ReSharper disable once InconsistentNaming
        public static IServiceCollection AddRabbitMQ(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<Consumer, RabbitMQCosumer>();
            //serviceCollection.AddTransient<IProducer>();
            
            return serviceCollection;
        }
    }
}