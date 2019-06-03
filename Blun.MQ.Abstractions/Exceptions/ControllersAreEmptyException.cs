using System;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Exceptions
{
    public class ControllersAreEmptyException : Exception
    {
        public ControllersAreEmptyException()
        {
        }

        public ControllersAreEmptyException(string message)
            : base(message)
        {
        }

        public ControllersAreEmptyException(string message, Exception inner)
            : base(message, inner)
        {
        }
        
        protected ControllersAreEmptyException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}