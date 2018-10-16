using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IDisposableAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RefCounterAnalyzer;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;

static internal class RightOfAssignmentUtils
{
	public static bool IsRightOfAssignmentToField(SemanticModel semanticModel, CancellationToken cancellationToken,
		SyntaxNode node, out ISymbol fieldOrProperty)
	{
		var left = semanticModel.GetSymbolSafe(node, cancellationToken) ??
		           semanticModel.GetSymbolSafe((node as ElementAccessExpressionSyntax)?.Expression,
			           cancellationToken);
	        

		bool fOrP = left is IFieldSymbol || left is IPropertySymbol;
		if (fOrP)
		{
			fieldOrProperty = left;
			return true;
		}
		fieldOrProperty = null;
		return false;
	}

	
	public static void ProcessRightOfAssignmentToField(
		ISymbol variable, 
		BlockSyntax block, 
		SemanticModel semanticModel, 
		CancellationToken cancellationToken, 
		RefCounterStatus status)
	{
		
		List<AssignmentExpressionSyntax> rc = new List<AssignmentExpressionSyntax>();
		block?.TryGetAssignment(variable, semanticModel, cancellationToken, rc);
		int count = 0;
		ISymbol field = null;
		Location loc = Location.None;
		foreach (AssignmentExpressionSyntax assignment in rc)
		{
			var classDef = block.FirstAncestor<ClassDeclarationSyntax>();
			var classSymbol = CSharpExtensions.GetDeclaredSymbol(semanticModel, classDef, cancellationToken);
			
			if (IsRightOfAssignmentToField(semanticModel, cancellationToken, assignment.Left, out field))
			{
				count++;
				loc = assignment.GetLocation();
				if (!field.ContainingType.Equals(classSymbol))
				{
					status.Skip("assigned to field/property of other class", loc);
				}

				if (LoopUtils.HasLoopBetween(assignment, block))
				{
					status.Skip("loop between assignment to field/property and var block", loc);
				}
			}

			
		}

		if (count == 1)
		{
			status.ReleaseReference("assign to class field/property", loc);

			var methodBlock = block?.FirstAncestorOrSelf<BlockSyntax>();
			if (methodBlock == null)
			{
				return;
			}

			if (RightSideOfAssignmentCount(field, methodBlock, semanticModel, cancellationToken) > 0)
			{
				status.Skip("var assigned to field/property, which is assigned to others", loc);
			}

			
			ChangeReferenceMethodUtils.ProcessIncDelRefInvocation(field, methodBlock, semanticModel, cancellationToken, status);
			ReturnUtils.ProcessReturnStatement(field, methodBlock, semanticModel, cancellationToken, status);
		}
		else if (count > 1)
		{
			status.Skip("multiple assign to field/property", loc);
		}
	}

	public static void ProcessRightOfAssignmentToLocal(ISymbol variable, 
		BlockSyntax block, SemanticModel semanticModel,
		CancellationToken cancellationToken, RefCounterStatus status)
	{
		List<AssignmentExpressionSyntax> rc = new List<AssignmentExpressionSyntax>();
		block?.TryGetAssignment(variable, semanticModel, cancellationToken, rc);
		foreach (AssignmentExpressionSyntax assignment in rc)
		{
			var left = semanticModel.GetSymbolSafe(assignment.Left, cancellationToken) ??
			           semanticModel.GetSymbolSafe((assignment.Left as ElementAccessExpressionSyntax)?.Expression, cancellationToken);
			bool fOrP = left is ILocalSymbol || left is IParameterSymbol;
			if (fOrP)
			{
				status.Skip("assigned to other local variables", assignment.GetLocation());
			}
		}
	}

	public static void ProcessRightSideOfAssignment(ISymbol local, BlockSyntax block, SemanticModel semanticModel,
		CancellationToken cancellationToken,
		RefCounterStatus status)
	{
		ProcessRightOfAssignmentToField(local, block, semanticModel, cancellationToken, status);

		ProcessRightOfAssignmentToLocal(local, block, semanticModel, cancellationToken, status);

	}

    public static int RightSideOfAssignmentCount(ISymbol variable,
        BlockSyntax block,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        int count = 0;
        using (var walker = AssignmentWalker.Borrow(block, Search.TopLevel, semanticModel, cancellationToken))
        {
            foreach (AssignmentExpressionSyntax assignment in walker.Assignments)
            {
                var right = semanticModel.GetSymbolSafe(CastUtils.UnWrapCast(assignment.Right), cancellationToken);
                if (right != null && right.Equals(variable))
                {
                    count++;
                }
            }
        }

        using (var walker = VariableDeclarationWalker.Borrow(block, Search.TopLevel, semanticModel, cancellationToken))
        {
            foreach (VariableDeclaratorSyntax delc in walker.Declarations)
            {
                var rightSymbol = semanticModel.GetSymbolSafe(CastUtils.UnWrapCast(delc.Initializer.Value), cancellationToken);
                if (SymbolComparer.Equals(variable, rightSymbol))
                {
                    count++;
                }
            }
        }
        return count;
    }
}