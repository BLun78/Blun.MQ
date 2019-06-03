using System;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Exceptions
{
    public class ControllerAreNullReferenceException : NullReferenceException
    {
        public ControllerAreNullReferenceException()
        {
        }

        public ControllerAreNullReferenceException(string message)
            : base(message)
        {
        }

        public ControllerAreNullReferenceException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected ControllerAreNullReferenceException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}
