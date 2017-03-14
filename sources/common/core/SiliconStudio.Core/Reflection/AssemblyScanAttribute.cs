﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiliconStudio.Core.Reflection
{
    /// <summary>
    /// This attribute can be either used on class or interfaces to scan for types inheriting from them, or on an attribute to scan for types having this specific attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
    public class AssemblyScanAttribute : Attribute
    {
    }
}
