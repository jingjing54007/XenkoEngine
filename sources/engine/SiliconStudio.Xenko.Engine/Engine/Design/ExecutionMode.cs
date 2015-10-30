using System;

namespace SiliconStudio.Xenko.Engine.Design
{
    /// <summary>
    /// Describes the different execution mode of the engine.
    /// </summary>
    [Flags]
    public enum ExecutionMode
    {
        None = 0,
        Runtime = 1,
        Editor = 2,
        All = Runtime | Editor,
    }
}