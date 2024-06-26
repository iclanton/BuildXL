// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.ContractsLight;
using BuildXL.FrontEnd.Script.Expressions;
using BuildXL.Utilities.Core;
using static BuildXL.Utilities.Core.FormattableStringEx;
using LineInfo = TypeScript.Net.Utilities.LineInfo;

namespace BuildXL.FrontEnd.Script.Declarations
{
    /// <summary>
    /// Configuration declaration.
    /// </summary>
    public class ConfigurationDeclaration : Declaration
    {
        /// <summary>
        /// Configuration keyword.
        /// </summary>
        /// <remarks>
        /// This ensures that the AST for manipulation and for evaluation in sync wrt. what the configuration is called.
        /// </remarks>
        public SymbolAtom ConfigKeyword { get; }

        /// <summary>
        /// Qualifier.
        /// </summary>
        public Expression ConfigExpression { get; }

        /// <nodoc />
        public ConfigurationDeclaration(
            SymbolAtom configKeyword,
            Expression configExpression,
            LineInfo location)
            : base(DeclarationFlags.None, location)
        {
            Contract.Requires(configKeyword.IsValid);
            Contract.Requires(configExpression != null);

            ConfigKeyword = configKeyword;
            ConfigExpression = configExpression;
        }

        /// <nodoc />
        public ConfigurationDeclaration(DeserializationContext context, LineInfo location)
            : base(context, location)
        {
            ConfigKeyword = ReadSymbolAtom(context);
            ConfigExpression = ReadExpression(context);
        }

        /// <inheritdoc />
        protected override void DoSerialize(BuildXLWriter writer)
        {
            base.DoSerialize(writer);

            WriteSymbolAtom(ConfigKeyword, writer);
            Serialize(ConfigExpression, writer);
        }

        /// <inheritdoc />
        public override void Accept(Visitor visitor)
        {
            visitor.Visit(this);
        }

        /// <inheritdoc />
        public override SyntaxKind Kind => SyntaxKind.ConfigurationDeclaration;

        /// <inheritdoc />
        public override string ToDebugString()
        {
            return I($"{ToDebugString(ConfigKeyword)}({ConfigExpression});");
        }

        /// <inheritdoc/>
        public override string ToStringShort(StringTable stringTable)
        {
            return I($"{ConfigKeyword.ToString(stringTable)}({ConfigExpression.ToStringShort(stringTable)});");
        }
    }
}
