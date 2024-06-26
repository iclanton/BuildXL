// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BuildXL.FrontEnd.Script.Evaluator;
using BuildXL.FrontEnd.Script.Values;
using TypeScript.Net.Utilities;

namespace BuildXL.FrontEnd.Script.Statements
{
    /// <summary>
    /// AST statement.
    /// </summary>
    public abstract class Statement : Node
    {
        /// <nodoc />
        protected Statement(LineInfo location)
            : base(location)
        {
        }

        /// <inheritdoc />
        protected override EvaluationResult DoEval(Context context, ModuleLiteral env, EvaluationStackFrame frame)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Evaluates a sequence of statements in order. If a statement returns an error, continue, break,
        /// or return value, then that value is returned. Otherwise, returns undefined.
        /// </summary>
        protected static EvaluationResult EvalStatements([NotNull]Context context, [NotNull]ModuleLiteral env, [NotNull]IReadOnlyList<Statement> statements, EvaluationStackFrame args)
        {
            for (int i = 0; i < statements.Count; ++i)
            {
                var s = statements[i];
                var v = s.Eval(context, env, args);

                // ErrorValue, Continue, Break, and Return should flow up the tree
                if (v.IsErrorValue
                    || v.Value == ContinueValue.Instance
                    || v.Value == BreakValue.Instance
                    || args.ReturnStatementWasEvaluated)
                {
                    return v;
                }
            }

            return EvaluationResult.Undefined;
        }
    }
}
