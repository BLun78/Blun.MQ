using Blun.MQ.AwsSQS.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Blun.MQ.AwsSQS
{
    // ReSharper disable once InconsistentNaming
    public static class AwsSQSExtension
    {
        // ReSharper disable once InconsistentNaming
        public static void AddAwsSQSAdapter(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<AwsSQSClientProxy>();
        }
    }
}