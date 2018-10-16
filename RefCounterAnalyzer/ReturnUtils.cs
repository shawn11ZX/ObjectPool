using System.Threading;
using IDisposableAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RefCounterAnalyzer
{
	public class ReturnUtils
	{
		public static void ProcessReturnStatement(ISymbol local, BlockSyntax block, SemanticModel semanticModel, CancellationToken cancellationToken,
			RefCounterStatus status)
		{
			using (var walker = ReturnValueWalker.Borrow(block, Search.TopLevel, semanticModel, cancellationToken))
			{
				foreach (var value in walker)
				{
                    var returnedSymbol = semanticModel.GetSymbolSafe(value, cancellationToken);
					if (SymbolComparer.Equals(local, returnedSymbol))
					{
					    var method = block.FirstAncestor<MethodDeclarationSyntax>();
					    var access = block.FirstAncestor<AccessorDeclarationSyntax>();
					    bool isGet = false;
					    if (method != null)
					    {
					        isGet = KnownSymbol.IsGetMethodName(method.Identifier.ToString()); 
					    }
                        else if (access != null)
					        isGet = true;
					    if (isGet)
					    {
					        status.RemainRef("return value from get method", value.GetLocation());
					    }
					    else
					    {
					        status.ReleaseReference("return value from non get", value.GetLocation());
					    }
                    }
				}
			}
			
		}

		
	}
}