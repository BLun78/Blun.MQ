using System;

namespace Blun.MQ.Exceptions
{
    public class ControllerAreEmptyException : Exception
    {
        public ControllerAreEmptyException()
        {
        }

        public ControllerAreEmptyException(string message)
            : base(message)
        {
        }

        public ControllerAreEmptyException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
