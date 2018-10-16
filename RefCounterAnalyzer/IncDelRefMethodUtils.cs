using System.Threading;
using IDisposableAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RefCounterAnalyzer
{
	public class IncDelRefMethodUtils
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
						    method?.Name == KnownSymbol.DelRef)
						{
							count++;
							loc = invocation.GetLocation();
							if (LoopUtils.HasLoopBetween(invocation, block))
							{
								status.Skip("loop between DelRef call and var block", invocation.GetLocation());
							}
						}
					}
				}
			}

			if (count > 1)
			{
				status.Skip("more than one DelRef", loc);
			}
			else if (count == 1)
			{
				status.DelRef("call DelRef", loc);
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
						    method?.Name == KnownSymbol.IncRef)
						{
							count++;
							loc = invocation.GetLocation();
							if (LoopUtils.HasLoopBetween(invocation, block))
							{
								status.Skip("loop between IncRef call and var block", loc);
							}
						}
					}
				}
			}

			if (count > 1)
			{
				status.Skip("more than one IncRef", loc);
			}
			else if (count == 1)
			{
				status.IncRef("call IncRef", loc);
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