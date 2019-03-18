using System;
using System.Reflection;

namespace Blun.MQ.Messages
{
    internal interface IMessageDefinitionAssemblyInfos
    {
        QueueRoutingAttribute QueueRouting { get; }
        MessageRouteAttribute MessageRoute { get; }
        Type ControllerType { get; }
        MethodInfo MethodInfo { get; }
    }
}