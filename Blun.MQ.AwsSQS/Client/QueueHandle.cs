using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Blun.MQ.Abstractions;
using Blun.MQ.Context;
using Blun.MQ.Messages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Message = Blun.MQ.Messages.Message;

namespace Blun.MQ.AwsSQS.Client
{
    internal sealed class QueueHandle : IDisposable, IAsyncDisposable
    {
        public EventHandler<ReceiveMessageFromQueueEventArgs> MessageFromQueueReceived;
        public bool IsListening;

        private readonly string _queueName;
        private AmazonSQSClient _amazonSqsClient;
        private readonly AmazonSQSConfig _amazonSqsConfig;

        public QueueHandle(string queueName, ILoggerFactory loggerFactory)
        {
            _queueName = queueName;
            IsListening = false;
            _amazonSqsConfig = new AmazonSQSConfig();
        }

        public void OnReceiveMessageFromQueueEventArgs(ReceiveMessageFromQueueEventArgs e)
        {
            MessageFromQueueReceived?.Invoke(this, e);
        }

        public async Task<MQResponse> SendAsync(MQRequest mqRequest)
        {
            var sqsRequest = new SendMessageRequest($"{_amazonSqsConfig.ServiceURL}{_queueName}",
                JsonConvert.SerializeObject(mqRequest.Message));

            var result = await _amazonSqsClient.SendMessageAsync(sqsRequest).ConfigureAwait(false);

            return new MQResponse
            {
                HttpStatusCode = result.HttpStatusCode,
                Message = new Message(result.MessageId)
            };
        }

        public void StartLongPollingForResponse(CancellationToken cancellationToken)
        {
            _ = Task.Run(async () =>
            {
                await this.ListenLoop(cancellationToken).ConfigureAwait(false);
                IsListening = false;

            }, cancellationToken);

            IsListening = true;
        }

        private async Task ListenLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ReceiveMessageResponse sqsMessageResponse = null;

                try
                {
                    sqsMessageResponse = await GetMessages(cancellationToken).ConfigureAwait(false);
                    var messageCount = sqsMessageResponse.Messages.Count;
                }
                catch (OperationCanceledException ex)
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
                            if (cancellationToken.IsCancellationRequested)
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

        private async Task<ReceiveMessageResponse> GetMessages(CancellationToken cancellationToken)
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
                    using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, receiveTimeout.Token))
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
                Message = new Messages.Message(
                    message.MessageId,
                    message.Attributes,
                    message.Body)
            };

            OnReceiveMessageFromQueueEventArgs(messageEvent);
        }

        public void Connect()
        {
            _amazonSqsClient = new AmazonSQSClient(_amazonSqsConfig);
        }

        public void Dispose()
        {
            _amazonSqsClient?.Dispose();
        }

        public Task DisposeAsync([Optional] CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
