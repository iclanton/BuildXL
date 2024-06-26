// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.ContractsLight;
using TypeScript.Net.Parsing;
using TypeScript.Net.Types;

namespace BuildXL.FrontEnd.Script.RuntimeModel.AstBridge.Rules
{
    /// <summary>
    /// Rule for inline importFrom(...) and importFile(...) to enforce arguments and environment
    /// </summary>
    internal abstract class EnforceWellShapedInlineImportBase : LanguageRule
    {
        protected EnforceWellShapedInlineImportBase()
        {
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
                this,
                CheckInlineImportPath,
                TypeScript.Net.Types.SyntaxKind.CallExpression);
        }

        protected abstract void ValidateImportFrom(IExpression argument, [AllowNull]ILiteralExpression stringLiteral, DiagnosticContext context);

        protected abstract void DoValidateImportFile(IExpression argument, [AllowNull]ILiteralExpression stringLiteral, DiagnosticContext context);

        protected void ValidateImportFile(IExpression argument, [AllowNull] ILiteralExpression stringLiteral, DiagnosticContext context)
        {
            DoValidateImportFile(argument, stringLiteral, context);

            (_, ILiteralExpression literal, _, _) = argument.As<ITaggedTemplateExpression>();

            string text = stringLiteral?.Text ?? literal?.Text;
            if (text!= null && text.IndexOfAny(ImportPathHelpers.InvalidPathChars) != -1)
            {
                context.Logger.ReportModuleSpecifierContainsInvalidCharacters(
                    context.LoggingContext,
                    argument.LocationForLogging(context.SourceFile),
                    text,
                    ImportPathHelpers.InvalidPathCharsText);
            }
        }

        private void CheckInlineImportPath(INode node, DiagnosticContext context)
        {
            Contract.Requires(node != null);
            Contract.Requires(context != null);

            if (node.IsImportCall(
                out _,
                out var importKind,
                out IExpression argumentAsExpression,
                out ILiteralExpression argumentAsLiteral))
            {
                if (importKind == DScriptImportFunctionKind.ImportFrom)
                {
                    ValidateImportFrom(argumentAsExpression, argumentAsLiteral, context);
                }
                else if (importKind == DScriptImportFunctionKind.ImportFile)
                {
                    ValidateImportFile(argumentAsExpression, argumentAsLiteral, context);
                }
            }
        }
    }
}
