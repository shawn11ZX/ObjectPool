using System.Collections.Generic;
using System.Threading;
using IDisposableAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static internal class LeftOfAssignmentUtils
{
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
                    AssignmentUtils.CalcAssignmentValue(assignment.Right, block, semanticModel, cancellationToken, status);
                }
            }
        }
		

	}
}