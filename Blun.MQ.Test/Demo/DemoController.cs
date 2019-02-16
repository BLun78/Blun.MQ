using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.Test.Demo
{
    [Queue("Demo")]
    public class DemoController : MQController
    {
        private ILogger<DemoController> _logger;

        public DemoController(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DemoController>();
            _logger.LogTrace("Ctor: DemoController is created!");
        }

        [Message("HelloWorld")]
        public string HelloWorld(string hello)
        {
            var result = $"Hello {hello}!";
            _logger.LogTrace($"HelloWorld Message: {result}");
            return result;
        }

        [Message("HelloWorldAsync")]
        public Task<string> HelloWorldAsync(string hello)
        {
            var result = $"Hello {hello}!";
            _logger.LogTrace($"HelloWorldAsync Message: {result}");
            return Task.FromResult(result);
        }
    }
}
