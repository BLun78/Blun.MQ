﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Blun.MQ.Messages;
using Newtonsoft.Json;

namespace Blun.MQ.AwsSQS
{
    internal class QueueHandle: IDisposable
    {
        public EventHandler<ReceiveMessageFromQueueEventArgs> MessageFromQueueReceived;
        public bool IsListening;

        private readonly string _queueName;
        private AmazonSQSClient _amazonSqsClient;
        private readonly AmazonSQSConfig _amazonSqsConfig;
        
        public QueueHandle(string queueName)
        {
            _queueName = queueName;
            IsListening = false;
            _amazonSqsConfig = new AmazonSQSConfig();
        }

        public virtual void OnReceiveMessageFromQueueEventArgs(ReceiveMessageFromQueueEventArgs e)
        {
            MessageFromQueueReceived?.Invoke(this, e);
        }
        
        public async Task<SendMessageResponse> SendAsync<T>(T message)
        {
            var request = new SendMessageRequest(_amazonSqsConfig.ServiceURL + _queueName,
                JsonConvert.SerializeObject(message));

            var result = await _amazonSqsClient.SendMessageAsync(request).ConfigureAwait(false);

            return result;
        }

        public void StartLongPollingForResponse(CancellationToken cancellationToken)
        {
            _ = Task.Run(async () =>
            {
                await this.ListenLoop(cancellationToken).ConfigureAwait(false);
                IsListening = false;

            }, cancellationToken);

            IsListening = true;

            OnReceiveMessageFromQueueEventArgs(null);
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
                QueueUrl = _amazonSqsConfig.ServiceURL+ this._queueName,
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
    }
}
