using System;
using System.ComponentModel;
using Blun.MQ.Messages;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    internal class MessageReceivedEventArgs : AsyncCompletedEventArgs, IMessageDefinition, IMessageDefinitionResponseInfo
    {
        private readonly IMessageDefinitionResponseInfo _messageResponseInfo;

        public MessageReceivedEventArgs(
            [NotNull] IMessageDefinitionResponseInfo messageResponseInfo,
            [CanBeNull] Exception error,
            bool cancelled,
            [CanBeNull] object userState)
            : base(error, cancelled, userState)
        {
            _messageResponseInfo = messageResponseInfo ?? throw new ArgumentNullException(nameof(messageResponseInfo));
        }

        public MessageReceivedEventArgs(
            [NotNull] Message message,
            [NotNull] IMessageDefinitionResponseInfo messageResponseInfo,
            [CanBeNull] Exception error,
            bool cancelled,
            [CanBeNull] object userState)
            : this(messageResponseInfo, error, cancelled, userState)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }


        public string QueueName => _messageResponseInfo.QueueName;

        public string MessageName => _messageResponseInfo.MessageName;

        public string ReplayTo => _messageResponseInfo.ReplayTo;

        public string Key => $"{this.QueueName}.{this.MessageName}";

        public Message Message { get; internal set; }
    }
}