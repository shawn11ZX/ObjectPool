namespace IDisposableAnalyzers
{
    using System.Collections.Generic;
    using System.Threading;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class VariableDeclarationWalker : ExecutionWalker<VariableDeclarationWalker>
    {
        private readonly List<VariableDeclaratorSyntax> _declarations = new List<VariableDeclaratorSyntax>();

        private VariableDeclarationWalker()
        {
        }

        /// <summary>
        /// Gets a list with all <see cref="AssignmentExpressionSyntax"/> in the scope.
        /// </summary>
        public IReadOnlyList<VariableDeclaratorSyntax> Declarations => this._declarations;

	    public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
	    {
			this._declarations.Add(node);
		    base.VisitVariableDeclarator(node);
		}
	   

		internal static VariableDeclarationWalker Borrow(SyntaxNode node, Search search, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var walker = Borrow(() => new VariableDeclarationWalker());
            walker.SemanticModel = semanticModel;
            walker.CancellationToken = cancellationToken;
            walker.Search = search;
            walker.Visit(node);
            return walker;
        }

       

		protected override void Clear()
        {
            this._declarations.Clear();
            this.SemanticModel = null;
            this.CancellationToken = CancellationToken.None;
            base.Clear();
        }
    }
}
