// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.ContractsLight;
using BuildXL.FrontEnd.Script.Evaluator;
using BuildXL.FrontEnd.Script.Values;
using BuildXL.Utilities.Core;
using LineInfo = TypeScript.Net.Utilities.LineInfo;

namespace BuildXL.FrontEnd.Script.Expressions
{
    /// <summary>
    /// Symbol reference that uses full name as a symbol identity.
    /// </summary>
    /// <remarks>
    /// In some cases instead of using full location, full name should be used to find
    /// already resolved entry.
    /// There is no production use cases and this symbol references is being used only
    /// by tests.
    /// For instance, testing infrastructure allows to evaluate expression by its name
    /// and to resolve that expression, full name is used.
    /// </remarks>
    public sealed class FullNameBasedSymbolReference : SymbolReferenceExpression
    {
        /// <nodoc />
        public FullSymbol FullName { get; }

        /// <nodoc/>
        public FullNameBasedSymbolReference(FullSymbol fullName, LineInfo location)
            : base(location)
        {
            Contract.Requires(fullName.IsValid);
            FullName = fullName;
        }

        /// <nodoc />
        public FullNameBasedSymbolReference(DeserializationContext context, LineInfo location)
            : base(location)
        {
            FullName = context.Reader.ReadFullSymbol();
        }

        /// <inheritdoc />
        protected override void DoSerialize(BuildXLWriter writer)
        {
            writer.Write(FullName);
        }

        /// <inheritdoc />
        public override void Accept(Visitor visitor)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override SyntaxKind Kind => SyntaxKind.FullNameBasedSymbolReference;

        /// <inheritdoc />
        public override string ToDebugString()
        {
            return ToDebugString(FullName);
        }

        /// <inheritdoc />
        protected override EvaluationResult DoEval(Context context, ModuleLiteral env, EvaluationStackFrame frame)
        {
            return env.EvaluateEntryByFullName(context, FullName, Location);
        }
    }
}
