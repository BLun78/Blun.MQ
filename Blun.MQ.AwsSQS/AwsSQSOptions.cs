using System;
using System.Collections.Generic;
using System.Text;

namespace Blun.MQ.AwsSQS
{
    // ReSharper disable once InconsistentNaming
    public sealed class AwsSQSOptions
    {
        public AwsSQSOptions()
        {
        }

        public string AccessKey { get;  set; }
        public string SecretKey { get;  set; }
    }
}
