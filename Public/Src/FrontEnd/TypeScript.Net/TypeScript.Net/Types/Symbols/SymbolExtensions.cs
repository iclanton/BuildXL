// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TypeScript.Net.Types
{
    /// <summary>
    /// Set of extension methods for <see cref="ISymbol"/> interface.
    /// </summary>
    public static class SymbolExtensions
    {
        /// <summary>
        /// Returns the first declaration of a given symbol skipping DScript injected nodes.
        /// </summary>
        [return: MaybeNull]
        public static IDeclaration GetFirstDeclarationOrDefault([NotNull]this ISymbol symbol)
        {
            foreach (var d in symbol.DeclarationList)
            {
                if (d != null)
                {
                    return d;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the declarations of a given symbol skipping DScript injected nodes.
        /// </summary>
        [return: NotNull]
        public static IEnumerable<IDeclaration> GetDeclarations([NotNull]this ISymbol symbol)
        {
            foreach (var d in symbol.DeclarationList)
            {
                if (!d.IsInjectedForDScript())
                {
                    yield return d;
                }
            }
        }

        [return: NotNull]
        internal static ISymbolTable GetMembers(this ISymbol symbol)
        {
            // This trick allows to leave ISymbol.Members to be readonly.
            if (symbol.Members != null)
            {
                return symbol.Members;
            }

            Symbol @this = (Symbol)symbol;
            lock (@this)
            {
                if (@this.Members == null)
                {
                    @this.Members = SymbolTable.Create();
                }
            }

            return @this.Members;
        }

        [return: NotNull]
        internal static ISymbolTable GetExports(this ISymbol symbol)
        {
            // This trick allows to leave ISymbol.Members to be readonly.
            if (symbol.Exports != null)
            {
                return symbol.Exports;
            }

            Symbol @this = (Symbol)symbol;
            lock (@this)
            {
                if (@this.Exports == null)
                {
                    @this.Exports = SymbolTable.Create();
                }
            }

            return @this.Exports;
        }

        [return: NotNull]
        internal static ISymbol GetOriginalSymbolOrSelf(this ISymbol symbol)
        {
            return symbol.OriginalSymbol ?? symbol;
        }
    }
}
