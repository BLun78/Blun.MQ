using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Framework
[assembly: InternalsVisibleTo("Blun.MQ")]

// Adapter
[assembly: InternalsVisibleTo("Blun.MQ.AmqpNetLite")]
[assembly: InternalsVisibleTo("Blun.MQ.AwsSQS")]
[assembly: InternalsVisibleTo("Blun.MQ.RabbitMQ")]

// Test
[assembly: InternalsVisibleTo("Blun.MQ.MockClient")]
[assembly: InternalsVisibleTo("Blun.MQ.Test")]