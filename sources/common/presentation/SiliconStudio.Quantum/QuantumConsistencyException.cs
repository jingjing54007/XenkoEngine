﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;

namespace SiliconStudio.Quantum
{
    /// <summary>
    /// An exception that occurs during consistency checks of Quantum objects, indicating that a <see cref="IContentNode"/> is un an unexpected state.
    /// </summary>
    public class QuantumConsistencyException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the QuantumConsistencyException class.
        /// </summary>
        /// <param name="expected">A string representing the expected result.</param>
        /// <param name="observed">A string representing the observed result.</param>
        /// <param name="node">The node that is related to this error.</param>
        public QuantumConsistencyException(string expected, string observed, IContentNode node)
            : base(GetMessage(expected, observed))
        {
            Expected = expected ?? "(NullMessage)";
            Observed = observed ?? "(NullMessage)";
            Node = node;
        }

        /// <summary>
        /// Initializes a new instance of the QuantumConsistencyException class, with advanced string formatting.
        /// </summary>
        /// <param name="expected">A string representing the expected result. This string must contains a </param>
        /// <param name="expectedArg"></param>
        /// <param name="observed">A string representing the observed result.</param>
        /// <param name="observedArg"></param>
        /// <param name="node">The node that is related to this error.</param>
        public QuantumConsistencyException(string expected, string expectedArg, string observed, string observedArg, IContentNode node)
            : base(GetMessage(expected, expectedArg, observed, observedArg))
        {
            try
            {
                Expected = string.Format(expected ?? "(NullMessage) [{0}]", expectedArg ?? "(NullArgument)");
            }
            catch (Exception)
            {
                Expected = expected ?? "(NullMessage) [{0}]";
            }
            try
            {
                Observed = string.Format(observed ?? "(NullMessage) [{0}]", observedArg ?? "(NullArgument)");
            }
            catch (Exception)
            {
                Observed = observed ?? "(NullMessage) [{0}]";
            }

            Node = node;
        }

        /// <summary>
        /// Gets a string representing the expected result.
        /// </summary>
        public string Expected { get; private set; }

        /// <summary>
        /// Gets a string representing the observed result.
        /// </summary>
        public string Observed { get; private set; }

        /// <summary>
        /// Gets the <see cref="IContentNode"/> that triggered this exception.
        /// </summary>
        public IContentNode Node { get; private set; }

        ///// <inheritdoc/>
        //public override string ToString()
        //{
        //    return GetMessage(Expected, Observed);
        //}

        private static string Format(string message, string argument)
        {
            try
            {
                return string.Format(message ?? "(NullMessage) [{0}]", argument ?? "(NullArgument)");
            }
            catch (Exception)
            {
                return message ?? "(NullMessage) [(NullArgument)]";
            }

        }

        private static string Format(string message)
        {
            return message ?? "(NullMessage)";

        }

        private static string GetMessage(string expected, string observed)
        {
            return string.Format("Quantum consistency exception. Expected: {0} - Observed: {1}", Format(expected), Format(observed));
        }

        private static string GetMessage(string expected, string expectedArg, string observed, string observedArg)
        {
            return string.Format("Quantum consistency exception. Expected: {0} - Observed: {1}", Format(expected, expectedArg), Format(observed, observedArg));
        }
    }
}