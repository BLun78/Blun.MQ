namespace Blun.MQ.Message
{
    // ReSharper disable once InconsistentNaming
    internal sealed class MQContextFactory
    {
        public MQContext CreateContext(IMessageDefinition messageDefinition)
        {
            var context = new MQContext();


            return context;
        }
    }
}
