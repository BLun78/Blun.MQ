using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Amqp;
using Amqp.Framing;
using JetBrains.Annotations;

namespace Blun.MQ.AmqpNetLite
{
    internal class AmqpNetLiteConsumer : Consumer
    {
        private readonly IOptionsMonitor<AmqpNetLiteOptions> _config;
        private readonly ILogger<AmqpNetLiteConsumer> _logger;
        private CancellationToken _cancellationToken;
        private Connection _connection;
        private Session _session;
        private Address _address;
        private KeyValuePair<string, IEnumerable<IMessageDefinition>> _queuesAndMessages;

        public AmqpNetLiteConsumer(
            ILoggerFactory loggerFactory,
            IOptionsMonitor<AmqpNetLiteOptions> config
            )
        {
            _config = config;
            _logger = loggerFactory.CreateLogger<AmqpNetLiteConsumer>();
        }
        
        public override async Task SetupQueueHandleAsync(
            [NotNull] KeyValuePair<string, IEnumerable<IMessageDefinition>> queuesAndMessages,
            [NotNull] CancellationToken cancellationToken)
        {
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));
            _queuesAndMessages = queuesAndMessages;
            _cancellationToken = cancellationToken;

            _address = new Address(_config.CurrentValue.Uri);
            _connection = await Connection.Factory.CreateAsync(_address).ConfigureAwait(true);
        }

        public override async Task StartListenerAsync(CancellationToken cancellationToken)
        {
            _session = new Session(_connection);
            var receiver = new ReceiverLink(_session, "Interop.Server-receiver", _queuesAndMessages.Key);
            int linkId = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                Amqp.Message request = await receiver.ReceiveAsync().ConfigureAwait(false);
                if (null != request)
                {
                    receiver.Accept(request);
                    string replyTo = request.Properties.ReplyTo;
                    
                    // TODO : ReplyTo kommt später
                    var sender = new SenderLink(_session, "Interop.Server-sender-" + (++linkId).ToString(), replyTo);

                    var response = new Amqp.Message();
                    response.Properties = new Properties() { CorrelationId = request.Properties.MessageId };

                    try
                    {
                        await sender.SendAsync(response).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        throw;
                    }
                    await sender.CloseAsync().ConfigureAwait(false);
                }
                else
                {
                    // timed out waiting for request. This is normal.
                    Console.WriteLine("Timeout waiting for request. Keep waiting...");
                }
            }
        }

        public override void Dispose()
        {
            _session.Close();
            _connection.Close();
        }
    }
}