using Microsoft.Extensions.DependencyInjection;

namespace Blun.MQ.AwsSQS
{
    public static class SQSExtension
    {
        public static void addAwsSQSAdapter(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<AwsSQSClientProxy>();
        }
    }
}