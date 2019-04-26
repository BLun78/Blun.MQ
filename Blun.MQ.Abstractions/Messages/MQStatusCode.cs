using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Blun.MQ.Messages
{
    // ReSharper disable once InconsistentNaming
    [DataContract(Name = "MQStatusCode")]
    internal enum MQStatusCode
    {
        [EnumMember(Value = "Ok")]
        Ok = 200,
        
        [EnumMember(Value = "NotOk")]
        NotOk = 400,
        
        [EnumMember(Value = "Error")]
        Error = 500
    }
}