﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Runtime.CompilerServices;
using SiliconStudio.Core.Annotations;

namespace SiliconStudio.Core.Diagnostics
{
    /// <summary>
    /// A class to store <a href="http://msdn.microsoft.com/en-us/library/hh534540%28v=vs.110%29.aspx">Caller Information</a> attributes.
    /// </summary>
    public sealed class CallerInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallerInfo" /> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="lineNumber">The line number.</param>
        private CallerInfo(string filePath, string memberName, int lineNumber)
        {
            FilePath = filePath;
            MemberName = memberName;
            LineNumber = lineNumber;
        }

        /// <summary>
        /// Full path of the source file that contains the caller. This is the file path at compile time.
        /// </summary>
        public readonly string FilePath;

        /// <summary>
        /// Method or property name of the caller. See Member Names later in this topic.
        /// </summary>
        public readonly string MemberName;

        /// <summary>
        /// Line number in the source file at which the method is called.
        /// </summary>
        public readonly int LineNumber;

        /// <summary>
        /// Gets the caller information.
        /// </summary>
        /// <param name="sourceFilePath">The source file path.</param>
        /// <param name="memberName">Name of the member.</param>
        /// <param name="sourceLineNumber">The source line number.</param>
        /// <returns>A caller information.</returns>
        [NotNull]
        public static CallerInfo Get([CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new CallerInfo(sourceFilePath, memberName, sourceLineNumber);
        }

        public override string ToString()
        {
            return $"{FilePath}:{MemberName}:{LineNumber}";
        }
    }
}
