namespace IDisposableAnalyzers
{
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

	internal static partial class RefCounter
    {
        internal static bool IsPotentiallyAssignableTo(ExpressionSyntax candidate, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (candidate == null ||
                candidate.IsMissing ||
                candidate is LiteralExpressionSyntax)
            {
                return false;
            }

            if (candidate is ObjectCreationExpressionSyntax objectCreation)
            {
                return IsAssignableTo(semanticModel.GetTypeInfoSafe(objectCreation, cancellationToken).Type);
            }

            return IsPotentiallyAssignableTo(semanticModel.GetTypeInfoSafe(candidate, cancellationToken).Type);
        }

        internal static bool IsPotentiallyAssignableTo(ITypeSymbol type)
        {
            if (type == null ||
                type is IErrorTypeSymbol)
            {
                return false;
            }

            if (type.IsValueType &&
                !IsAssignableTo(type))
            {
                return false;
            }

            if (type.IsSealed &&
                !IsAssignableTo(type))
            {
                return false;
            }

            return true;
        }

        internal static bool IsAssignableTo(ITypeSymbol type)
        {
            if (type == null)
            {
                return false;
            }

            if (type is ITypeParameterSymbol typeParameter)
            {
                foreach (var constraintType in typeParameter.ConstraintTypes)
                {
                    if (IsAssignableTo(constraintType))
                    {
                        return true;
                    }
                }

                return false;
            }

            return type == KnownSymbol.RefCounter ||
                   type.Is(KnownSymbol.RefCounter);
        }

        internal static bool TryGetDelRefMethod(ITypeSymbol type, Search search, out IMethodSymbol delRefMethod)
        {
            delRefMethod = null;
            if (type == null)
            {
                return false;
            }

            var delers = type.GetMembers(KnownSymbol.ReleaseReference);
            switch (delers.Length)
            {
                case 0:
                    var baseType = type.BaseType;
                    if (search == Search.Recursive &&
                        IsAssignableTo(baseType))
                    {
                        return TryGetDelRefMethod(baseType, Search.Recursive, out delRefMethod);
                    }

                    return false;
                case 1:
                    delRefMethod = delers[0] as IMethodSymbol;
                    if (delRefMethod == null)
                    {
                        return false;
                    }

                    return (delRefMethod.Parameters.Length == 0 &&
                            delRefMethod.DeclaredAccessibility == Accessibility.Public) ||
                           (delRefMethod.Parameters.Length == 1 &&
                            delRefMethod.Parameters[0].Type == KnownSymbol.Boolean);
                case 2:
                    if (delers.TrySingle(x => (x as IMethodSymbol)?.Parameters.Length == 1, out ISymbol temp))
                    {
                        delRefMethod = temp as IMethodSymbol;
                        return delRefMethod != null &&
                               delRefMethod.Parameters[0].Type == KnownSymbol.Boolean;
                    }

                    break;
            }

            return false;
        }

        internal static bool TryGetBaseVirtualDelRefMethod(ITypeSymbol type, out IMethodSymbol result)
        {
            bool IsVirtualDelRef(IMethodSymbol m)
            {
                return m.IsVirtual &&
                       m.ReturnsVoid &&
                       m.Parameters.Length == 1 &&
                       m.Parameters[0].Type == KnownSymbol.Boolean;
            }

            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.TrySingleMethodRecursive(KnownSymbol.ReleaseReference, IsVirtualDelRef, out result))
                {
                    return true;
                }

                baseType = baseType.BaseType;
            }

            result = null;
            return false;
        }

	    public static bool IsRefCounterType(SemanticModel semanticModel, TypeSyntax type)
	    {
	        var symbolInfo = semanticModel.GetSymbolInfo(type);

	        return IsRefCounterType(symbolInfo.Symbol);
	    }

        public static bool IsRefCounterType(ISymbol symbolInfo)
        {
            bool isRefCounter = false;
            if (symbolInfo is ITypeSymbol typeInfo)
            {
                if (RefCounter.IsAssignableTo(typeInfo))
                {
                    isRefCounter = true;
                }
            }
            return isRefCounter;
        }
    }
}