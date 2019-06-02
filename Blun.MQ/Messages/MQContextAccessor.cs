using System.Threading;

namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    internal sealed class MQContextAccessor : IMQContextAccessor
    {
        // ReSharper disable once InconsistentNaming
        private static readonly AsyncLocal<MQContextHolder> MQContextHolderCurrent = new AsyncLocal<MQContextHolder>();

        public IMQContext MQContext
        {
            get
            {
                return MQContextHolderCurrent.Value?.Context;
            }
            set
            {
                var holder = MQContextHolderCurrent.Value;
                if (holder != null)
                {
                    // Clear current MessageContext trapped in the AsyncLocals, as its done.
                    holder.Context = null;
                }

                if (value != null)
                {
                    // Use an object indirection to hold the MessageContext in the AsyncLocal,
                    // so it can be cleared in all ExecutionContexts when its cleared.
                    MQContextHolderCurrent.Value = new MQContextHolder() { Context = value };
                }
            }
        }

        // ReSharper disable once InconsistentNaming
        private class MQContextHolder
        {
            public IMQContext Context;
        }
    }
}