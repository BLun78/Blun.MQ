using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Blun.MQ.Context;
using Blun.MQ.Messages;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Message = Blun.MQ.Messages.Message;

namespace Blun.MQ.AwsSQS.Client
{
    internal sealed class QueueHandle : IDisposable
    {
        public EventHandler<ReceiveMessageFromQueueEventArgs> MessageFromQueueReceived;
        public bool IsListening;

        private readonly string _queueName;
        private readonly IEnumerable<IMessageDefinition> _messageDefinitions;
        private readonly CancellationToken _cancellationToken;
        private readonly AmazonSQSClient _amazonSqsClient;
        private readonly AmazonSQSConfig _amazonSqsConfig;
        private readonly ILogger<QueueHandle> _logger;

        public QueueHandle([NotNull, ItemNotNull]IEnumerable<IMessageDefinition> messageDefinitions,
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] CancellationToken cancellationToken)
        {
            _logger = loggerFactory.CreateLogger<QueueHandle>();
            _messageDefinitions = messageDefinitions;
            _queueName = messageDefinitions.First().QueueName;
            _cancellationToken = cancellationToken;
            IsListening = false;
            _amazonSqsConfig = new AmazonSQSConfig();
            _amazonSqsClient = new AmazonSQSClient(_amazonSqsConfig);
        }
        
        public async Task<MQResponse> SendAsync(MQRequest mqRequest)
        {
            var sqsRequest = new SendMessageRequest($"{_amazonSqsConfig.ServiceURL}{_queueName}",
                JsonConvert.SerializeObject(mqRequest.Message));

            var result = await _amazonSqsClient.SendMessageAsync(sqsRequest, _cancellationToken).ConfigureAwait(false);

            return new MQResponse
            {
                HttpStatusCode = result.HttpStatusCode,
                Message = new Message(result.MessageId)
            };
        }

        public void CreateQueueListener()
        {
            StartLongPollingForResponse();
        }
        
        public void Dispose()
        {
            _amazonSqsClient?.Dispose();
        }

        private void StartLongPollingForResponse()
        {
            _ = Task.Run(async () =>
            {
                await this.ListenLoop().ConfigureAwait(false);
                IsListening = false;

            }, _cancellationToken);

            IsListening = true;
        }

        private void OnReceiveMessageFromQueueEventArgs(ReceiveMessageFromQueueEventArgs e)
        {
            MessageFromQueueReceived?.Invoke(this, e);
        }

        private async Task ListenLoop()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                ReceiveMessageResponse sqsMessageResponse = null;

                try
                {
                    sqsMessageResponse = await GetMessages().ConfigureAwait(false);
                    var messageCount = sqsMessageResponse.Messages.Count;
                }
                catch (OperationCanceledException oce)
                {

                }
                catch (Exception e)
                {

                }

                try
                {
                    if (sqsMessageResponse != null)
                    {
                        foreach (var message in sqsMessageResponse.Messages)
                        {
                            if (_cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }

                            HandleMessage(message);
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        private async Task<ReceiveMessageResponse> GetMessages()
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = _amazonSqsConfig.ServiceURL + this._queueName,
                MaxNumberOfMessages = 200,
                WaitTimeSeconds = 20,
            };

            using (var receiveTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(300)))
            {
                ReceiveMessageResponse sqsMessageResponse;

                try
                {
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, receiveTimeout.Token))
                    {
                        sqsMessageResponse = await _amazonSqsClient.ReceiveMessageAsync(request, linkedCts.Token)
                            .ConfigureAwait(false);
                    }
                }
                finally
                {
                    if (receiveTimeout.Token.IsCancellationRequested)
                    {

                    }
                }

                return sqsMessageResponse;
            }
        }

        private void HandleMessage(Amazon.SQS.Model.Message message)
        {
            var messageEvent = new ReceiveMessageFromQueueEventArgs
            {
                QueueName = _queueName,
                MessageName = message.ReceiptHandle,
                Message = new Messages.Message(
                    message.MessageId,
                    message.Attributes,
                    message.Body)
            };

            OnReceiveMessageFromQueueEventArgs(messageEvent);
        }
        
    }
}
