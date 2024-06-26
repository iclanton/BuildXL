// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.ContractsLight;
using BuildXL.FrontEnd.Script.Evaluator;
using BuildXL.FrontEnd.Script.Expressions;
using BuildXL.FrontEnd.Script.Values;
using TypeScript.Net.Utilities;
using static BuildXL.Utilities.Core.FormattableStringEx;

namespace BuildXL.FrontEnd.Script.Ambients
{
    /// <summary>
    /// Exception that occurs when an ambient encounters an error with a directory operation.
    /// </summary>
    public sealed class DirectoryOperationException : EvaluationException
    {
        /// <summary>
        /// Wrapped exception.
        /// </summary>
        public Exception WrappedException { get; }

        /// <summary>
        /// Directory name.
        /// </summary>
        public string Directory { get; }

        /// <nodoc />
        public DirectoryOperationException(string directory, Exception wrappedException)
        {
            Contract.Requires(wrappedException != null);

            Directory = directory;
            WrappedException = wrappedException;
        }

        /// <inheritdoc/>
        public override void ReportError(EvaluationErrors errors, ModuleLiteral environment, LineInfo location, Expression expression, Context context)
        {
            string additionalInformation =
                string.IsNullOrEmpty(WrappedException.Message)
                    ? string.Empty
                    : I($": {Directory} -- {WrappedException.Message}");

            errors.ReportDirectoryOperationError(environment, expression, additionalInformation, location);
        }
    }
}
