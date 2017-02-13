﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;

namespace SiliconStudio.Core.Diagnostics
{
    /// <summary>
    /// A logger that stores messages locally with their timestamp, useful for internal log scenarios.
    /// </summary>
    public class TimestampLocalLogger : Logger
    {
        private readonly DateTime startTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerResult" /> class.
        /// </summary>
        public TimestampLocalLogger(DateTime startTime, string moduleName = null)
        {
            this.startTime = startTime;

            Module = moduleName;
            Messages = new List<Message>();

            // By default, all logs are enabled for a local logger.
            ActivateLog(LogMessageType.Verbose);
        }

        /// <summary>
        /// Gets the messages logged to this instance.
        /// </summary>
        /// <value>The messages.</value>
        public List<Message> Messages { get; }

        protected override void LogRaw(ILogMessage logMessage)
        {
            var timestamp = DateTime.Now - startTime;
            lock (Messages)
            {
                Messages.Add(new Message(timestamp.Ticks, logMessage));
            }
        }

        /// <summary>
        /// A structure describing a log message associated with a timestamp.
        /// </summary>
        public struct Message
        {
            /// <summary>
            /// The timestamp associated to the log message.
            /// </summary>
            public long Timestamp;

            /// <summary>
            /// The log message.
            /// </summary>
            public ILogMessage LogMessage;

            /// <summary>
            /// Structure constructor.
            /// </summary>
            /// <param name="timestamp">The timestamp associated to the log message.</param>
            /// <param name="logMessage">The log message.</param>
            public Message(long timestamp, ILogMessage logMessage) { Timestamp = timestamp; LogMessage = logMessage; }
        }
    }
}
