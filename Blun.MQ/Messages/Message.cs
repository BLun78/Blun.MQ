using System;
using System.Collections.Generic;
using System.Text;

namespace Blun.MQ.Messages
{
    public class Message
    {
        public Header Header { get; set; }

        public Body Body { get; set; }
    }
}
