// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.ContractsLight;
using System.Globalization;
using System.IO;
using System.Text;
using BuildXL.FrontEnd.Script.Evaluator;
using BuildXL.FrontEnd.Script.Values;
using BuildXL.Utilities.Core;
using BuildXL.Utilities.Instrumentation.Common;
using BuildXL.Utilities.Core.Qualifier;
using static BuildXL.Utilities.Core.FormattableStringEx;

namespace BuildXL.FrontEnd.Script
{
    /// <nodoc />
    public static class DisplayStringExtensions
    {
        /// <nodoc />
        public static string ToDisplayString(this Type type, ImmutableContextBase context)
        {
            return new DisplayStringHelper(context).TypeToString(type);
        }

        /// <nodoc />
        public static string ToDisplayString(this Node node, ImmutableContextBase context)
        {
            Contract.Requires(node != null);
            Contract.Requires(context != null);

            using (var writer = new StringWriter(new StringBuilder(), CultureInfo.InvariantCulture))
            {
                try
                {
                    return PrettyPrinter.GetLogStringRepr(
                        context.FrontEndContext,
                        writer,
                        context.LastActiveUsedPath.GetParent(context.FrontEndContext.PathTable),
                        context.Package.Path.GetParent(context.FrontEndContext.PathTable),
                        node);
                }
                catch (NotImplementedException)
                {
                    // Some AST values have no implementation of visitor.
                    // TODO: Fix me!
                    return I($"<ValueExpression:({node.Location.Line},{node.Location.Position})>");
                }
            }
        }

        /// <nodoc />
        public static string ToDisplayString(this Location location)
        {
            return location.Line != -1
                ? I($"{location.File}({location.Line.ToString(CultureInfo.InvariantCulture)},{location.Position.ToString(CultureInfo.InvariantCulture)})")
                : location.File;
        }

        /// <nodoc />
        public static string ToDisplayString(this QualifierId qualifierId, ImmutableContextBase context)
        {
            Contract.Requires(context != null);

            if (!qualifierId.IsValid)
            {
                return "Invalid";
            }

            Qualifier qualifier = context.FrontEndContext.QualifierTable.GetQualifier(qualifierId);
            return qualifier.ToDisplayString(context.FrontEndContext.StringTable);
        }

        /// <nodoc />
        public static string ToDisplayString(this QualifierSpaceId qualifierSpaceId, ImmutableContextBase context)
        {
            Contract.Requires(context != null);

            if (!qualifierSpaceId.IsValid)
            {
                return "Invalid";
            }

            QualifierSpace qualifierSpace = context.FrontEndContext.QualifierTable.GetQualifierSpace(qualifierSpaceId);
            return qualifierSpace.ToDisplayString(context.FrontEndContext.StringTable);
        }

        /// <nodoc />
        public static string ToDisplayString(this SymbolAtom atom, ImmutableContextBase context) => ToDisplayString(atom, context.StringTable);

        /// <nodoc />
        public static string ToDisplayString(this SymbolAtom atom, StringTable stringTable)
        {
            if (!atom.IsValid)
            {
                return "undefined";
            }

            return atom.ToString(stringTable);
        }

        /// <nodoc />
        public static string ToDisplayString(this AbsolutePath path, ImmutableContextBase context)
        {
            if (!path.IsValid)
            {
                return "undefined";
            }

            return path.ToString(context.FrontEndContext.PathTable);
        }

        /// <nodoc />
        public static string ToDisplayString(this ModuleLiteralId moduleLiteralId, ImmutableContextBase context)
        {
            return new DisplayStringHelper(context).ToString(moduleLiteralId);
        }

        /// <nodoc />
        public static string ToDisplayString(this FullSymbol fullSymbol, ImmutableContextBase context)
        {
            return new DisplayStringHelper(context).ToString(fullSymbol);
        }
        
        /// <nodoc />
        public static string ToDisplayString(this FullSymbol fullSymbol, SymbolTable symbolTable)
        {
            return DisplayStringHelper.ToString(fullSymbol, symbolTable);
        }
    }
}
