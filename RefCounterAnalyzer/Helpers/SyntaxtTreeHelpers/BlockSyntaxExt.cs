using System.Collections.Generic;

namespace IDisposableAnalyzers
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class BlockSyntaxExt
    {
        internal static bool TryGetReturnExpression(this BlockSyntax body, SemanticModel semanticModel, CancellationToken cancellationToken, out ExpressionSyntax returnValue)
        {
            return ReturnValueWalker.TrySingle(body, semanticModel, cancellationToken, out returnValue);
        }

        internal static bool TryGetAssignment(this BlockSyntax body, ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken, out AssignmentExpressionSyntax result)
        {
            result = null;
            if (symbol == null)
            {
                return false;
            }

            return AssignmentWalker.FirstWith(symbol, body, Search.TopLevel, semanticModel, cancellationToken, out result);
        }

	    internal static bool TryGetAssignment(this BlockSyntax body, ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken, List<AssignmentExpressionSyntax> result)
	    {
		    if (symbol == null)
		    {
			    return false;
		    }
				
		    AssignmentWalker.AllWith(symbol, body, Search.TopLevel, semanticModel, cancellationToken, result);
		    return true;
	    }
	}
}