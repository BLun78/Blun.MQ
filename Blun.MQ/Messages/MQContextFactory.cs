using System;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    internal sealed class MQContextFactory
    {
        public MQContext CreateContext(
            [NotNull] IServiceScope serviceScope,
            [NotNull] MessageReceivedEventArgs eventArgs)
        {
            var context = new MQContext
            {
                MQRequest = eventArgs.CreateMQRequest()
            };
            
            return context;
        }
    }
}