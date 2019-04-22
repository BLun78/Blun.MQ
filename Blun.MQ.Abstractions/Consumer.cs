﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Blun.MQ.Messages;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Blun.MQ
{
    internal abstract class Consumer : IDisposable
    {
        public event MessageReceivedEventHandler MessageReceived;

        public delegate void MessageReceivedEventHandler(object sender, MessageReceivedEventArgs e);

        public abstract Task SetupQueueHandleAsync(IMessageDefinitionResponseInfo messageResponseInfo, CancellationToken cancellationToken);
        public abstract Task StartListenerAsync();

        protected virtual void OnMessageReceived(MessageReceivedEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        public abstract void Dispose();
    }
}