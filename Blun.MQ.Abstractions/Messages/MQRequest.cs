﻿using Blun.MQ.Messages;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    public class MQRequest {

        public Message Message { get; internal set; }

        public string QueueRoute { get; internal set; }

        public string MessageRoute { get; internal set; }

        public long ContentLength { get; internal set; }
    }
}