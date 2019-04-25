using Microsoft.Extensions.DependencyInjection;

namespace Blun.MQ.AmqpNetLite
{
    // ReSharper disable once InconsistentNaming
    public static class AmqpNetLiteExtension
    {
        // ReSharper disable once InconsistentNaming
        public static void AddAmqpNetLiteAdapter(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<AmqpNetLiteConsumer>();
        }
    }
}