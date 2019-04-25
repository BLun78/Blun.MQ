using System;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Amqp;
using Amqp.Framing;
using Amqp.Types;

namespace Blun.MQ.AmqpNetLite
{
    internal class AmqpNetLiteConsumer : Consumer
    {
        private readonly IOptionsMonitor<AmqpNetLiteOptions> _config;
        private readonly ILogger<AmqpNetLiteConsumer> _logger;
        private IMessageDefinitionResponseInfo _messageResponseInfo;
        private CancellationToken _cancellationToken;
        private Connection _connection;
        private Session _session;
        private Address _address;

        public AmqpNetLiteConsumer(
            ILoggerFactory loggerFactory,
            IOptionsMonitor<AmqpNetLiteOptions> config
            )
        {
            _config = config;
            _logger = loggerFactory.CreateLogger<AmqpNetLiteConsumer>();
        }
        
        public override async Task SetupQueueHandleAsync(IMessageDefinitionResponseInfo messageResponseInfo, CancellationToken cancellationToken)
        {
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));
            if (messageResponseInfo == null) throw new ArgumentNullException(nameof(messageResponseInfo));
            _messageResponseInfo = messageResponseInfo;
            _cancellationToken = cancellationToken;

            _address = new Address(_config.CurrentValue.Uri);
            _connection = await Connection.Factory.CreateAsync(_address).ConfigureAwait(false);
            
        }

        public override Task StartListenerAsync()
        {
            _session = new Session(_connection);
            var receiver = new ReceiverLink(_session, "Interop.Server-receiver", _messageResponseInfo.QueueName);
            int linkId = 0;
            while (true)
            {
                Amqp.Message request = receiver.Receive();
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
                        sender.Send(response);
                    }
                    catch (Exception exception)
                    {
                        throw;
                    }
                    sender.Close();
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