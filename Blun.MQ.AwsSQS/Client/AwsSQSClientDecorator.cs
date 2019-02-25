using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.Runtime.SharedInterfaces;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;

namespace Blun.MQ.AwsSQS.Client
{
    // ReSharper disable once InconsistentNaming
    internal sealed class AwsSQSClientDecorator : IAmazonSQS,  ICoreAmazonSQS, IDisposable
    {
        private readonly IOptionsMonitor<AwsSQSOptions> AwsSqsOptions;
        private readonly AmazonSQSClient _amazonSqsClient;
        private readonly AmazonSQSConfig _amazonSqsConfig;

        public AwsSQSClientDecorator(IOptionsMonitor<AwsSQSOptions> awsSqsOptions)
        {
            AwsSqsOptions = awsSqsOptions;
            var options = awsSqsOptions.CurrentValue;
            _amazonSqsConfig = new AmazonSQSConfig()
            {

            };
            _amazonSqsClient = new AmazonSQSClient(new BasicAWSCredentials("","")
            {
                
            } ,_amazonSqsConfig);
        }

        public void Dispose()
        {
            _amazonSqsClient.Dispose();
        }

        public Task<Dictionary<string, string>> GetAttributesAsync(string queueUrl)
        {
            return ((ICoreAmazonSQS) _amazonSqsClient).GetAttributesAsync(queueUrl);
        }

        public Task SetAttributesAsync(string queueUrl, Dictionary<string, string> attributes)
        {
            return ((ICoreAmazonSQS) _amazonSqsClient).SetAttributesAsync(queueUrl, attributes);
        }

        public IClientConfig Config => ((IAmazonService) _amazonSqsClient).Config;

        public Task<string> AuthorizeS3ToSendMessageAsync(string queueUrl, string bucket)
        {
            return _amazonSqsClient.AuthorizeS3ToSendMessageAsync(queueUrl, bucket);
        }

        public Task<AddPermissionResponse> AddPermissionAsync(string queueUrl, string label, List<string> awsAccountIds, List<string> actions,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.AddPermissionAsync(queueUrl, label, awsAccountIds, actions, cancellationToken);
        }

        public Task<AddPermissionResponse> AddPermissionAsync(AddPermissionRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.AddPermissionAsync(request, cancellationToken);
        }

        public Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int visibilityTimeout,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.ChangeMessageVisibilityAsync(queueUrl, receiptHandle, visibilityTimeout, cancellationToken);
        }

        public Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(ChangeMessageVisibilityRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.ChangeMessageVisibilityAsync(request, cancellationToken);
        }

        public Task<ChangeMessageVisibilityBatchResponse> ChangeMessageVisibilityBatchAsync(string queueUrl, List<ChangeMessageVisibilityBatchRequestEntry> entries,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.ChangeMessageVisibilityBatchAsync(queueUrl, entries, cancellationToken);
        }

        public Task<ChangeMessageVisibilityBatchResponse> ChangeMessageVisibilityBatchAsync(ChangeMessageVisibilityBatchRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.ChangeMessageVisibilityBatchAsync(request, cancellationToken);
        }

        public Task<CreateQueueResponse> CreateQueueAsync(string queueName, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.CreateQueueAsync(queueName, cancellationToken);
        }

        public Task<CreateQueueResponse> CreateQueueAsync(CreateQueueRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.CreateQueueAsync(request, cancellationToken);
        }

        public Task<DeleteMessageResponse> DeleteMessageAsync(string queueUrl, string receiptHandle,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.DeleteMessageAsync(queueUrl, receiptHandle, cancellationToken);
        }

        public Task<DeleteMessageResponse> DeleteMessageAsync(DeleteMessageRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.DeleteMessageAsync(request, cancellationToken);
        }

        public Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(string queueUrl, List<DeleteMessageBatchRequestEntry> entries,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.DeleteMessageBatchAsync(queueUrl, entries, cancellationToken);
        }

        public Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(DeleteMessageBatchRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.DeleteMessageBatchAsync(request, cancellationToken);
        }

        public Task<DeleteQueueResponse> DeleteQueueAsync(string queueUrl, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.DeleteQueueAsync(queueUrl, cancellationToken);
        }

        public Task<DeleteQueueResponse> DeleteQueueAsync(DeleteQueueRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.DeleteQueueAsync(request, cancellationToken);
        }

        public Task<GetQueueAttributesResponse> GetQueueAttributesAsync(string queueUrl, List<string> attributeNames,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.GetQueueAttributesAsync(queueUrl, attributeNames, cancellationToken);
        }

        public Task<GetQueueAttributesResponse> GetQueueAttributesAsync(GetQueueAttributesRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.GetQueueAttributesAsync(request, cancellationToken);
        }

        public Task<GetQueueUrlResponse> GetQueueUrlAsync(string queueName, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.GetQueueUrlAsync(queueName, cancellationToken);
        }

        public Task<GetQueueUrlResponse> GetQueueUrlAsync(GetQueueUrlRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.GetQueueUrlAsync(request, cancellationToken);
        }

        public Task<ListDeadLetterSourceQueuesResponse> ListDeadLetterSourceQueuesAsync(ListDeadLetterSourceQueuesRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.ListDeadLetterSourceQueuesAsync(request, cancellationToken);
        }

        public Task<ListQueuesResponse> ListQueuesAsync(string queueNamePrefix, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.ListQueuesAsync(queueNamePrefix, cancellationToken);
        }

        public Task<ListQueuesResponse> ListQueuesAsync(ListQueuesRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.ListQueuesAsync(request, cancellationToken);
        }

        public Task<ListQueueTagsResponse> ListQueueTagsAsync(ListQueueTagsRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.ListQueueTagsAsync(request, cancellationToken);
        }

        public Task<PurgeQueueResponse> PurgeQueueAsync(string queueUrl, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.PurgeQueueAsync(queueUrl, cancellationToken);
        }

        public Task<PurgeQueueResponse> PurgeQueueAsync(PurgeQueueRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.PurgeQueueAsync(request, cancellationToken);
        }

        public Task<ReceiveMessageResponse> ReceiveMessageAsync(string queueUrl, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.ReceiveMessageAsync(queueUrl, cancellationToken);
        }

        public Task<ReceiveMessageResponse> ReceiveMessageAsync(ReceiveMessageRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.ReceiveMessageAsync(request, cancellationToken);
        }

        public Task<RemovePermissionResponse> RemovePermissionAsync(string queueUrl, string label,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.RemovePermissionAsync(queueUrl, label, cancellationToken);
        }

        public Task<RemovePermissionResponse> RemovePermissionAsync(RemovePermissionRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.RemovePermissionAsync(request, cancellationToken);
        }

        public Task<SendMessageResponse> SendMessageAsync(string queueUrl, string messageBody,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.SendMessageAsync(queueUrl, messageBody, cancellationToken);
        }

        public Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.SendMessageAsync(request, cancellationToken);
        }

        public Task<SendMessageBatchResponse> SendMessageBatchAsync(string queueUrl, List<SendMessageBatchRequestEntry> entries,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.SendMessageBatchAsync(queueUrl, entries, cancellationToken);
        }

        public Task<SendMessageBatchResponse> SendMessageBatchAsync(SendMessageBatchRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.SendMessageBatchAsync(request, cancellationToken);
        }

        public Task<SetQueueAttributesResponse> SetQueueAttributesAsync(string queueUrl, Dictionary<string, string> attributes,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.SetQueueAttributesAsync(queueUrl, attributes, cancellationToken);
        }

        public Task<SetQueueAttributesResponse> SetQueueAttributesAsync(SetQueueAttributesRequest request,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.SetQueueAttributesAsync(request, cancellationToken);
        }

        public Task<TagQueueResponse> TagQueueAsync(TagQueueRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.TagQueueAsync(request, cancellationToken);
        }

        public Task<UntagQueueResponse> UntagQueueAsync(UntagQueueRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            return _amazonSqsClient.UntagQueueAsync(request, cancellationToken);
        }
    }
}
