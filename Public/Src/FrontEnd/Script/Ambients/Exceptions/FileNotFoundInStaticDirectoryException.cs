// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.ContractsLight;
using BuildXL.FrontEnd.Script.Evaluator;
using BuildXL.FrontEnd.Script.Expressions;
using BuildXL.FrontEnd.Script.Values;
using TypeScript.Net.Utilities;
using static BuildXL.Utilities.Core.FormattableStringEx;

namespace BuildXL.FrontEnd.Script.Ambients
{
    /// <summary>
    /// Exception that is thrown when provided file is not present in a static directory.
    /// </summary>
    public sealed class FileNotFoundInStaticDirectoryException : EvaluationExceptionWithErrorContext
    {
        /// <nodoc />
        public FileNotFoundInStaticDirectoryException(string absolutePathToUknownFile, ErrorContext errorContext)
            : base (
               I($"Could not find file '{absolutePathToUknownFile}' in static directory."),
               errorContext)
        {
            Contract.Requires(!string.IsNullOrEmpty(absolutePathToUknownFile));
            AbsolutePathToUnknownFile = absolutePathToUknownFile;
        }

        /// <summary>
        /// Path to a file that wasn't found in a static directory.
        /// </summary>
        public string AbsolutePathToUnknownFile { get; }

        /// <inheritdoc/>
        public override void ReportError(
            EvaluationErrors errors,
            ModuleLiteral environment,
            LineInfo location,
            Expression expression,
            Context context)
        {
            errors.ReportFileNotFoundInStaticDirectory(environment, AbsolutePathToUnknownFile, location);
        }
    }
}
