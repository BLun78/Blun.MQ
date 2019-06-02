using System;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    internal sealed class MQContextFactory
    {
        public MQContext CreateContext([NotNull] IServiceScope serviceScope, MessageReceivedEventArgs eventArgs)
        {
            var context = new MQContext
            {
                MQRequest = eventArgs.CreateMQRequest()
            };
            if (serviceScope.ServiceProvider.GetService<IMQContextAccessor>() is MQContextAccessor mqMessageContext)
            {
                mqMessageContext.MQContext = context;
            }
            return context;
        }
    }
}