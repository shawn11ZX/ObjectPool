using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static internal class CastUtils
{
    public static SyntaxNode UnWrapCast(ExpressionSyntax node)
    {
        if (node is CastExpressionSyntax cast)
        {
            return UnWrapCast(cast.Expression);
        }
        else
        {
            return node;
        }
    }
}