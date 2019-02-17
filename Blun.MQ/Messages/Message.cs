using System;
using System.Collections.Generic;
using System.Text;

namespace Blun.MQ.Messages
{
    public class Message
    {
        public Header Header { get; internal set; }

        public Body Body { get; internal set; }
    }
}
