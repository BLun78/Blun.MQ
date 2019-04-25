using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Blun.MQ.Messages;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Example.MQControllers
{
    [QueueRouting("HelloWorld")]
    public class HelloWorldController : MQController
    {
        public HelloWorldController(ILogger<MQController> logger) : base(logger)
        {
        }

        [UsedImplicitly]
        [MessageRoute("StringBased")]
        public IMQResponse MessageBased(string message)
        {
            return Ok("");
        }

        [UsedImplicitly]
        [MessageRoute("StringBasedAsync")]
        public async Task<IMQResponse> MessageBasedAsync(string message)
        {
            await Task.Delay(1).ConfigureAwait(false);
            return Ok("");
        }

        [UsedImplicitly]
        [MessageRoute("MessageBased")]
        public IMQResponse MessageBased(Message message)
        {
            return Ok(message);
        }

        [UsedImplicitly]
        [MessageRoute("MessageBasedAsync")]
        public async Task<IMQResponse> MessageBasedAsync(Message message)
        {
            await Task.Delay(1).ConfigureAwait(false);
            return Ok(message);
        }

        [UsedImplicitly]
        [MessageRoute("MessageBasedOhneParameter")]
        public IMQResponse MessageBasedOhneParameter(Message message)
        {
            return Ok();
        }

        [UsedImplicitly]
        [MessageRoute("MessageBasedOhneParameterAsync")]
        public async Task<IMQResponse> MessageBasedOhneParameterAsync(Message message)
        {
            await Task.Delay(1).ConfigureAwait(false);
            return Ok();
        }


        [UsedImplicitly]
        [MessageRoute("IMQRequestBased")]
        public IMQResponse IMQRequestBased(IMQRequest request)
        {
            return Ok(request);
        }


        [UsedImplicitly]
        [MessageRoute("IMQRequestBasedAsync")]
        public async Task<IMQResponse> IMQRequestBasedAsync(IMQRequest request)
        {
            await Task.Delay(1).ConfigureAwait(false);
            return Ok(request);
        }

        [UsedImplicitly]
        [MessageRoute("IMQRequestBasedOhneParameter")]
        public IMQResponse IMQRequestBasedOhneParameter(IMQRequest request)
        {
            return Ok();
        }


        [UsedImplicitly]
        [MessageRoute("IMQRequestBasedOhneParameterAsync")]
        public async Task<IMQResponse> IMQRequestBasedOhneParameterAsync(IMQRequest request)
        {
            await Task.Delay(1).ConfigureAwait(false);
            return Ok();
        }
    }
}