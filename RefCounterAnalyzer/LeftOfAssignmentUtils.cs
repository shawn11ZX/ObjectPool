using System.Collections.Generic;
using System.Threading;
using IDisposableAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static internal class LeftOfAssignmentUtils
{
	public static bool IsRightSideOfAssignment(ISymbol variable,
		BlockSyntax block,
		SemanticModel semanticModel,
		CancellationToken cancellationToken)
	{
	    using (var walker = AssignmentWalker.Borrow(block, Search.TopLevel, semanticModel, cancellationToken))
	    {
	        foreach (AssignmentExpressionSyntax assignment in walker.Assignments)
	        {
	            var left = semanticModel.GetSymbolSafe(assignment.Right, cancellationToken);
	            if (left != null && left.Equals(variable))
	            {
	                return true;
	            }
	        }
	    }

	    using (var walker = VariableDeclarationWalker.Borrow(block, Search.TopLevel, semanticModel, cancellationToken))
		{
			foreach (VariableDeclaratorSyntax delc in walker.Declarations)
			{
				var rightSymbol = semanticModel.GetSymbolSafe(delc.Initializer.Value, cancellationToken);
				if (SymbolComparer.Equals(variable, rightSymbol))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static void ProcessLeftSideOfAssignment(ISymbol variable, BlockSyntax block, SemanticModel semanticModel, CancellationToken cancellationToken, RefCounterStatus status)
	{
		List<AssignmentExpressionSyntax> rc = new List<AssignmentExpressionSyntax>();
	    using (var walker = AssignmentWalker.Borrow(block, Search.TopLevel, semanticModel, cancellationToken))
        {
            foreach (AssignmentExpressionSyntax assignment in walker.Assignments)
            {
                var sym = ModelExtensions.GetSymbolInfo(semanticModel, assignment.Left, cancellationToken);
                if (variable.Equals(sym.Symbol))
                {
                    AssignmentUtils.CalcAssignmentValue(assignment.Right, semanticModel, cancellationToken, status);
                }
            }
        }
		

	}
}