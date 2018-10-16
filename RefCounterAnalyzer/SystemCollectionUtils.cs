using System.Threading;
using IDisposableAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static internal class SystemCollectionUtils
{
	public static void ProcessAddedToCollection(
		ISymbol symbol, BlockSyntax block, SemanticModel semanticModel, CancellationToken cancellationToken, RefCounterStatus status)
	{
		int count = 0;
		Location loc = Location.None;
		using (var pooledInvocations = InvocationWalker.Borrow(block))
		{
			foreach (var invocation in pooledInvocations.Invocations)
			{
				bool isAddToCollection = false;
				var method = semanticModel.GetSymbolSafe(invocation, cancellationToken) as IMethodSymbol;
				if (method?.ReceiverType != null)
				{
					if (!isAddToCollection)
					{
						isAddToCollection = (method.ReceiverType.Is(KnownSymbol.IDictionary) || method.ReceiverType.Is(KnownSymbol.ICollection)) &&
						                    method?.Name == "Add";
					}

					if (!isAddToCollection)
					{
						isAddToCollection = method.ReceiverType.Is(KnownSymbol.QueueOfT) &&
						                    method?.Name == "Enqueue";
					}

					if (!isAddToCollection)
					{
						isAddToCollection = method.ReceiverType.Is(KnownSymbol.Queue) &&
						                    method?.Name == "Enqueue";
					}

				    if (!isAddToCollection)
				    {
				        isAddToCollection = method.ReceiverType.Is(KnownSymbol.ListOfT) &&
				                            method?.Name == "Insert";
				    }
				    if (!isAddToCollection)
				    {
				        isAddToCollection = method.ReceiverType.Is(KnownSymbol.IRefCounterContainerOfT) &&
				                            method?.Name == "Add";
				    }

				    if (!isAddToCollection)
				    {
				        isAddToCollection = method.ReceiverType.Is(KnownSymbol.CollectionOfT) &&
				                            method?.Name == "Add";
				    }
				    if (!isAddToCollection)
				    {
				        isAddToCollection = method.ReceiverType.Is(KnownSymbol.LinkedListOfT) &&
				                            (method?.Name == "AddLast" || method?.Name == "AddFirst");
                        

                    }

                }

				if (isAddToCollection)
				{
					bool isInArg = false;
					foreach (ArgumentSyntax arg in invocation.ArgumentList.Arguments)
					{
						var sym = ModelExtensions.GetSymbolInfo(semanticModel, arg.Expression, cancellationToken);
						ISymbol argSymbol = sym.Symbol;
						if (symbol.Equals(argSymbol))
						{
							isInArg = true;
						}
					}

					if (isInArg)
					{
						count++;
						loc = invocation.GetLocation();
						if (LoopUtils.HasLoopBetween(invocation, block))
						{
							status.Skip("loop between add to collection and var block", invocation.GetLocation());
						}

					}
				}
			}
		}

		if (count == 1)
		{
			status.ReleaseReference("added to collection", loc);
		}
	}
}