// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using BuildXL.FrontEnd.Script.Util;
using BuildXL.FrontEnd.Workspaces;
using TypeScript.Net.Types;

namespace BuildXL.FrontEnd.Script.RuntimeModel.AstBridge.Rules
{
    internal static class LinterNodeExtensions
    {
        /// <summary>
        /// Returns true if the is mutable.
        /// </summary>
        /// <remarks>
        /// This implementation is naive and based on a type name.
        /// We know that there is no 'mutable' types in the prelude except some special one that should be restricted on top-level declarations.
        /// </remarks>
        public static bool IsMutable([AllowNull]string name)
        {
            return name?.Contains("Mutable") == true || name?.Contains("StringBuilder") == true;
        }

        /// <nodoc />
        public static bool IsMutable([AllowNull]this IType type)
        {
            if (IsMutable(type?.Symbol?.Name))
            {
                return true;
            }

            // If the type is generic, need to check type arguments
            var typeReference = type.As<ITypeReference>();
            if (typeReference != null && typeReference.TypeArguments?.Count > 0)
            {
                return typeReference.TypeArguments.Any(t => IsMutable(t));
            }

            return IsMutable(type?.Symbol?.Name);
        }

        /// <nodoc />
        public static bool IsReturnTypeMutable([AllowNull]this IFunctionDeclaration function, ISemanticModel semanticModel)
        {
            if (function == null)
            {
                return false;
            }

            var type = function.Type;
            string name = null;
            if (type != null)
            {
                var symbol = semanticModel.GetSymbolAtLocation(type);
                name = symbol?.Name;
            }

            if (name != null)
            {
                return IsMutable(name);
            }

            // Slow path
            var signature = semanticModel.TypeChecker.GetSignatureFromDeclaration(function);
            var returnType = semanticModel.TypeChecker.GetReturnTypeOfSignature(signature);

            return returnType?.IsMutable() == true;
        }

        /// <nodoc />
        public static bool HasMutableParameterType([AllowNull]this IFunctionDeclaration function, ISemanticModel semanticModel)
        {
            if (function == null)
            {
                return false;
            }

            var signature = semanticModel.TypeChecker.GetResolvedSignature(function);
            foreach (var p in signature.Parameters)
            {
                if (IsMutable(GetTypeOfSymbol(p, semanticModel)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <nodoc />
        public static bool IsExported([AllowNull] this IDeclaration declaration)
        {
            return (declaration?.Flags & NodeFlags.Export) == NodeFlags.Export;
        }

        private static IType GetTypeOfSymbol(ISymbol symbol, ISemanticModel semanticModel)
        {
            var declaration = symbol.DeclarationList.FirstOrDefault();
            if (declaration == null)
            {
                return null;
            }

            return semanticModel.GetTypeAtLocation(declaration);
        }
    }
}
