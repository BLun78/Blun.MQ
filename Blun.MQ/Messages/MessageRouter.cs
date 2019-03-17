using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

// ReSharper disable CheckNamespace
namespace Blun.MQ.Messages
{
    internal sealed class MessageRouter
    {
        private readonly ILogger<MessageRouter> _logger;

        public MessageRouter(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MessageRouter>();
        }


    }
}
