using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.Test.Demo
{
    [Queue("Demo")]
    [Queue("Demo2")]
    public class DemoController : MQController
    {
        private ILogger<DemoController> _logger;

        public DemoController(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DemoController>();
            _logger.LogTrace("Ctor: DemoController is created!");
        }

        [MessagePattern("HelloWorld")]
        [MessagePattern("HelloWorld2")]
        public string HelloWorld(string hello)
        {
            var result = $"Hello {hello}!";
            _logger.LogTrace($"HelloWorld Message: {result}");
            return result;
        }

        [MessagePattern("HelloWorldAsync")]
        [MessagePattern("HelloWorld2Async")]
        public Task<string> HelloWorldAsync(string hello)
        {
            var result = $"Hello {hello}!";
            _logger.LogTrace($"HelloWorldAsync Message: {result}");
            return Task.FromResult(result);
        }

        [MessagePattern("BugMethodNotFindInProvider")]
        [MessagePattern("BugMethodNotFindInProvider2")]
        private string BugMethodNotFindInProvider(string hello)
        {
            return "";
        }
    }
}
