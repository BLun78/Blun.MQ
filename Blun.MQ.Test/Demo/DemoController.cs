using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Blun.MQ.Test.Demo
{
    [QueueRouting("Demo")]
    [QueueRouting("Demo2")]
    public class DemoController : MQController
    {
        private readonly ILogger<DemoController> _logger;

        public DemoController(ILogger<DemoController> logger) 
            : base(logger)
        {
            _logger = logger;
            _logger.LogTrace("Ctor: DemoController is created!");
        }

        [UsedImplicitly]
        [MessageRoute("HelloWorld")]
        [MessageRoute("HelloWorld2")]
        public string HelloWorld(string hello)
        {
            var result = $"Hello {hello}!";
            _logger.LogTrace($"HelloWorld Message: {result}");
            return result;
        }

        [UsedImplicitly]
        [MessageRoute("HelloWorldAsync")]
        [MessageRoute("HelloWorld2Async")]
        public Task<string> HelloWorldAsync(string hello)
        {
            var result = $"Hello {hello}!";
            _logger.LogTrace($"HelloWorldAsync Message: {result}");
            return Task.FromResult(result);
        }

        [UsedImplicitly]
        [MessageRoute("BugMethodNotFindInProvider")]
        [MessageRoute("BugMethodNotFindInProvider2")]
        private string BugMethodNotFindInProvider(string hello)
        {
            return "";
        }
    }
}
