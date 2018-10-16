using System.Threading;
using IDisposableAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RefCounterAnalyzer
{
	public class ChangeReferenceMethodUtils
	{
		public static void ProcessIncDelRefInvocation(ISymbol local, BlockSyntax block, SemanticModel semanticModel, CancellationToken cancellationToken,
			RefCounterStatus status)
		{
		

			IncRefInvocationCount(local, block, semanticModel, cancellationToken, status);
		

			DelRefInvocationCount(local, block, semanticModel, cancellationToken, status);
		}

		public static int DelRefInvocationCount(ISymbol symbol, BlockSyntax block, SemanticModel semanticModel, CancellationToken cancellationToken, RefCounterStatus status)
		{
			int count = 0;
			Location loc = Location.None;;
			using (var pooledInvocations = InvocationWalker.Borrow(block))
			{
				foreach (var invocation in pooledInvocations.Invocations)
				{
					var isFromSymbol = IsInvokeFrom(symbol, semanticModel, cancellationToken, invocation);
					var method = semanticModel.GetSymbolSafe(invocation, cancellationToken) as IMethodSymbol;
				    if (isFromSymbol && method?.ReceiverType != null)
				    {
				        if (method.ReceiverType.Is(KnownSymbol.RefCounter) &&
				            method?.Name == KnownSymbol.ReleaseReference)
				        {
				            count++;
				            loc = invocation.GetLocation();
				            if (LoopUtils.HasLoopBetween(invocation, block))
				            {
				                status.Skip("loop between ReleaseReference call and var block", invocation.GetLocation());
				            }
				        }
				    }
				    else
				    {
				        if (method?.Name == KnownSymbol.ReleaseReference)
				        {
				            foreach (ArgumentSyntax arg in invocation.ArgumentList.Arguments)
				            {

				                var sym = ModelExtensions.GetSymbolInfo(semanticModel, arg.Expression, cancellationToken);
				                ISymbol argSymbol = sym.Symbol;
				                if (symbol.Equals(argSymbol))
				                {
				                    count++;
				                    loc = invocation.GetLocation();

				                    if (LoopUtils.HasLoopBetween(invocation, block))
				                    {
				                        status.Skip("loop between ReleaseReference call and var block", arg.GetLocation());
				                    }
				                }

				            }
				        }
				    }
                    
				}
			}

			if (count > 1)
			{
				status.Skip("more than one ReleaseReference", loc);
			}
			else if (count == 1)
			{
				status.ReleaseReference("call ReleaseReference", loc);
			}

			return count;
		}

	

		private static int IncRefInvocationCount(ISymbol symbol, BlockSyntax block, SemanticModel semanticModel, CancellationToken cancellationToken, RefCounterStatus status)
		{
			int count = 0;
			Location loc = Location.None;;
			using (var pooledInvocations = InvocationWalker.Borrow(block))
			{
				foreach (var invocation in pooledInvocations.Invocations)
				{
					var isFromSymbol = IsInvokeFrom(symbol, semanticModel, cancellationToken, invocation);

					var method = semanticModel.GetSymbolSafe(invocation, cancellationToken) as IMethodSymbol;
					if (isFromSymbol && method.ReceiverType != null)
					{
						if (method.ReceiverType.Is(KnownSymbol.RefCounter) &&
						    method?.Name == KnownSymbol.AcquireReference)
						{
							count++;
							loc = invocation.GetLocation();
							if (LoopUtils.HasLoopBetween(invocation, block))
							{
								status.Skip("loop between AcquireReference call and var block", loc);
							}
						}
					}
				}
			}

			if (count > 1)
			{
				status.Skip("more than one AcquireReference", loc);
			}
			else if (count == 1)
			{
				status.AcquireReference("call AcquireReference", loc);
			}

			return count;
		}


		private static bool IsInvokeFrom(ISymbol symbol, SemanticModel semanticModel, CancellationToken cancellationToken,
			InvocationExpressionSyntax invocation)
		{
			bool isFromSymbol = false;
			if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
			{
				var id = semanticModel.GetSymbolSafe(memberAccess.Expression, cancellationToken);
				if (symbol.Equals(id))
				{
					isFromSymbol = true;
				}
			}
			return isFromSymbol;
		}
	}
}