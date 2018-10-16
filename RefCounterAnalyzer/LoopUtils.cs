using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static internal class LoopUtils
{
	public static bool HasLoopBetween(SyntaxNode start, BlockSyntax end)
	{
		while (start != end && start != null)
		{
			if (start is ForEachStatementSyntax || start is WhileStatementSyntax || start is ForStatementSyntax ||
			    start is DoStatementSyntax)
			{
				return true;
			}
			start = start.Parent;
		}
		return false;
	}
}