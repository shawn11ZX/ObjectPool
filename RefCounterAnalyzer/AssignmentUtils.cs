using System.Collections.Generic;
using System.Threading;
using IDisposableAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RefCounterAnalyzer;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;

public class AssignmentUtils
{
	public static void ProcessVariableInitialization(
		VariableDeclaratorSyntax variable,
		SemanticModel semanticModel,
		CancellationToken cancellationToken,
		RefCounterStatus refCounterStatus)
	{

		if (variable.Initializer == null || variable.Initializer.Value == null) 
		{
			//refCounterStatus.Skip("not initialized while declare");
		}
		else
		{
			CalcAssignmentValue(variable.Initializer.Value, semanticModel, cancellationToken, refCounterStatus);
		}
	}

	public static void CalcAssignmentValue(ExpressionSyntax variable, SemanticModel semanticModel, CancellationToken cancellationToken,
		RefCounterStatus refCounterStatus)
	{
		
		if (InitializeFromCreation(variable))
		{
			refCounterStatus.IncAssignCounter("initialize from new", variable.GetLocation());
			refCounterStatus.IncRef("init from new", variable.GetLocation());
		}
		else if (InitializeFromNonGetMethod(variable, semanticModel))
		{
			refCounterStatus.IncAssignCounter("initialize from non-get method", variable.GetLocation());
			refCounterStatus.IncRef("init from non-get method", variable.GetLocation());
		}
		else if (InitializeFromGetMethod(variable, semanticModel))
		{
		    refCounterStatus.IncAssignCounter("initialize from get method", variable.GetLocation());
		    refCounterStatus.RemainRef("init from get method", variable.GetLocation());
		}
        else if (InitializeFromProperty(variable, semanticModel, cancellationToken))
		{
			refCounterStatus.IncAssignCounter("initialize from field/property", variable.GetLocation());
			refCounterStatus.RemainRef("init from property", variable.GetLocation());
			// nothing
		}
		else if (InitializeFromElement(variable, semanticModel, cancellationToken))
		{
		    refCounterStatus.IncAssignCounter("initialize from element access", variable.GetLocation());
		    refCounterStatus.RemainRef("init from element access", variable.GetLocation());
        }
        else // assigned from other local variable, class field, or other expression
		{
			refCounterStatus.IncAssignCounter("initialize from unkown", variable.GetLocation());
			refCounterStatus.Skip("unsupported initialization", variable.GetLocation());
		}
	}

    
    private static bool InitializeFromElement(ExpressionSyntax variable, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (variable is CastExpressionSyntax cast)
        {
            return InitializeFromElement(cast.Expression, semanticModel, cancellationToken);
        }
        if (variable is ElementAccessExpressionSyntax)
            return true;
        return false;
    }
    private static bool InitializeFromProperty(ExpressionSyntax variable, SemanticModel semanticModel, CancellationToken cancellationToken)
	{
		if (variable is CastExpressionSyntax cast)
		{
			return InitializeFromProperty(cast.Expression, semanticModel, cancellationToken);
		}
		if (variable is MemberAccessExpressionSyntax)
			return true;
		else if (variable is IdentifierNameSyntax idName)
		{
			return IsFieldOrProperty(semanticModel, cancellationToken, idName);
		}
		return false;
	}

	private static bool InitializeFromGetMethod(ExpressionSyntax variable, SemanticModel semanticModel)
	{
		if (variable is CastExpressionSyntax cast)
		{
			return InitializeFromGetMethod(cast.Expression, semanticModel);

		}
	    if (variable is InvocationExpressionSyntax invok)
	    {
	        return IsGetMethod(invok, semanticModel);

	    }
	    return false;
	}

    public static bool IsGetMethod(InvocationExpressionSyntax invok, SemanticModel semanticModel)
    {
        var sym = semanticModel.GetSymbolInfo(invok.Expression);
        if (sym.Symbol is IMethodSymbol method)
        {
            return method.Name.StartsWith(KnownSymbol.GetMethodProfix);
        }
        return false;
    }
    private static bool InitializeFromNonGetMethod(ExpressionSyntax variable, SemanticModel semanticModel)
    {
        if (variable is CastExpressionSyntax cast)
        {
            return InitializeFromNonGetMethod(cast.Expression, semanticModel);

        }
        if (variable is InvocationExpressionSyntax invok)
        {
            return !IsGetMethod(invok, semanticModel);

        }
        return false;
    }

    private static bool InitializeFromCreation(ExpressionSyntax variable)
	{
		return variable is ObjectCreationExpressionSyntax;
	}

	private static bool IsFieldOrProperty(SemanticModel semanticModel, CancellationToken cancellationToken, SyntaxNode node)
	{
		var left = semanticModel.GetSymbolSafe(node, cancellationToken) ??
		           semanticModel.GetSymbolSafe((node as ElementAccessExpressionSyntax)?.Expression,
			           cancellationToken);

		bool fOrP = left is IFieldSymbol || left is IPropertySymbol;
		if (fOrP)
		{
			return true;
		}
		return false;
	}

	public static void ProcessOutRefInvocation(ISymbol symbol, BlockSyntax block, SemanticModel semanticModel, CancellationToken cancellationToken, RefCounterStatus status)
	{
		using (var pooledInvocations = InvocationWalker.Borrow(block))
		{
			foreach (var invocation in pooledInvocations.Invocations)
			{
				
				foreach (ArgumentSyntax arg in invocation.ArgumentList.Arguments)
				{
					if (arg.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword) || arg.RefOrOutKeyword.IsKind(SyntaxKind.RefKeyword))
					{
						var sym = ModelExtensions.GetSymbolInfo(semanticModel, arg.Expression, cancellationToken);
						ISymbol argSymbol = sym.Symbol;
						if (symbol.Equals(argSymbol))
						{
						    status.RemainRef("init from out/ref call", arg.GetLocation());
                            status.IncAssignCounter("out/ref call parameter", arg.GetLocation());

							if (LoopUtils.HasLoopBetween(invocation, block))
							{
								status.Skip("loop between ref/out call and var block", arg.GetLocation());
							}
						}
					}
				}

			}
		}

	}


}