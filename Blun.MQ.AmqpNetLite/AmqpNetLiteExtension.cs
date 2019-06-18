using Microsoft.Extensions.DependencyInjection;

namespace Blun.MQ.AmqpNetLite
{
    // ReSharper disable once InconsistentNaming
    public static class AmqpNetLiteExtension
    {
        // ReSharper disable once InconsistentNaming
        public static void AddMqWithAmqpNetLiteAdapter(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<Consumer, AmqpNetLiteConsumer>();
            serviceCollection.AddSingleton<HandleRecievedMessage>();

            ServiceCollectionMQExtension.AddMQ(serviceCollection);
        }
    }
}