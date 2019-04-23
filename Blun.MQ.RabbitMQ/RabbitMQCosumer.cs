using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Blun.MQ.Messages;
using Blun.MQ.RabbitMQ.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Blun.MQ.RabbitMQ
{
    // ReSharper disable once InconsistentNaming
    internal class RabbitMQCosumer : Consumer, IDisposable
    {
        private readonly IOptionsMonitor<RabbitMQOptions> _config;
        private readonly ILogger<RabbitMQCosumer> _logger;
        private readonly ConnectionFactory _factory;
        private IConnection _connection;
        private IModel _channel;
        private CancellationToken _cancellationToken;
        private IMessageDefinitionResponseInfo _messageResponseInfo;


        public RabbitMQCosumer(
            ILoggerFactory loggerFactory,
            IOptionsMonitor<RabbitMQOptions> config)
        {
            _config = config;
            _logger = loggerFactory.CreateLogger<RabbitMQCosumer>();
            _factory = CreateConnectionFactory();
        }

        public override Task StartListenerAsync()
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += ConsumerOnReceived;
            _channel.BasicConsume(queue: _messageResponseInfo.QueueName, autoAck: false, consumer: consumer);

            return Task.CompletedTask;
        }

        public override Task SetupQueueHandleAsync(IMessageDefinitionResponseInfo messageResponseInfo, CancellationToken cancellationToken)
        {
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));
            if (messageResponseInfo == null) throw new ArgumentNullException(nameof(messageResponseInfo));
            _messageResponseInfo = messageResponseInfo;
            _cancellationToken = cancellationToken;

            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(_messageResponseInfo.QueueName, true, false, false, null);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.SetupQueueHandle(_messageResponseInfo.QueueName);

            return Task.CompletedTask;
        }

        public Task Ack()
        {
            return Task.CompletedTask;
        }
        
        public Task NotAck()
        {
            return Task.CompletedTask;
        }
            
        private void ConsumerOnReceived(object sender, [NotNull] BasicDeliverEventArgs e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            try
            {
                var body = e.Body;
                var messageString = Encoding.UTF8.GetString(body);
                var message = new Message();

                if (_cancellationToken.IsCancellationRequested)
                {
                    _channel.BasicNack(e.DeliveryTag, false, true);
                }
                else
                {
                    _channel.BasicAck(deliveryTag: e.DeliveryTag, multiple: false);
                    var messageReceivedEventArgs =
                        new MessageReceivedEventArgs(message, _messageResponseInfo, null,
                            _cancellationToken.IsCancellationRequested, null);
                    OnMessageReceived(messageReceivedEventArgs);
                }
            }
            catch (Exception exception)
            {
                OnMessageReceived(new MessageReceivedEventArgs(_messageResponseInfo, exception,
                    _cancellationToken.IsCancellationRequested, null));
                throw;
            }
        }

        private ConnectionFactory CreateConnectionFactory()
        {
            var connectionFactory = new ConnectionFactory();
            if (string.IsNullOrWhiteSpace(_config.CurrentValue.Uri))
            {
                SetConfig(connectionFactory);
            }
            else
            {
                connectionFactory.Uri = new Uri(_config.CurrentValue.Uri);
            }

            // connectionFactory.Ssl= new SslOption("servername, cert, true");

            return connectionFactory;
        }

        private void SetConfig(ConnectionFactory connectionFactory)
        {
            connectionFactory.UserName = _config.CurrentValue.UserName;
            connectionFactory.Password = _config.CurrentValue.Password;
            connectionFactory.HostName = _config.CurrentValue.HostName;
            connectionFactory.VirtualHost = _config.CurrentValue.VirtualHost;
            connectionFactory.Port = _config.CurrentValue.Port;
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}