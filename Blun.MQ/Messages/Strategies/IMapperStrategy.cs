﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
// ReSharper disable CheckNamespace

namespace Blun.MQ.Messages.Strategies
{
    internal interface IMapperStrategy
    {
        bool IsMappable(MethodInfo methodInfo);
    }
}
