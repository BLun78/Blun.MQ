using System;
using System.Threading.Tasks;
using Blun.MQ.Controllers;
using Blun.MQ.Messages;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Blun.MQ
{
    // ReSharper disable once InconsistentNaming
    public abstract partial class MQController : INotOk
    {
        public IMQResponse NotOk()
        {
            return CreateMQNullResponse(MQStatusCode.NotOk);
        }

        public IMQResponse NotOk([NotNull] string result)
        {
            if (string.IsNullOrWhiteSpace(result))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(result));

            return CreateMQResponse(result, MQStatusCode.NotOk);
        }

        public IMQResponse NotOk([NotNull] string result, [NotNull] Exception exception)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            throw new NotImplementedException();
        }

        public IMQResponse NotOk([NotNull] object result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            throw new NotImplementedException();
        }

        public IMQResponse NotOk([NotNull] object result, [NotNull] Exception exception)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            throw new NotImplementedException();
        }
    }
}