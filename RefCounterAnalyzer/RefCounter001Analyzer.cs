using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.FindSymbols;
using RefCounterAnalyzer;

namespace IDisposableAnalyzers
{
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

	// created but not assigned
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class RefCounter001 : DiagnosticAnalyzer
	{
		public const string RefCounterErrorDiagnosticId = "RefCounter001";
		public const string RefCounterSkipDiagnosticId = "RefCounter002";
	    public const string RefCounterParseErrorId = "RefCounter003";
	    public const string RefCounterNotAssignedErrorId = "RefCounter004";
        private const string Category = "RefCounter";
		// You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
		// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
		private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.RefCounter001AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
		private static readonly LocalizableString RefCounterErrorMessageFormat = new LocalizableResourceString(nameof(Resources.RefCounterErrorAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
		private static readonly LocalizableString RefCounterSkipMessageFormat = new LocalizableResourceString(nameof(Resources.RefCounterSkipAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
	    private static readonly LocalizableString RefCounterParseErroMessageFormat = new LocalizableResourceString(nameof(Resources.RefCounterParseErrorMessageFormat), Resources.ResourceManager, typeof(Resources));
	    private static readonly LocalizableString RefCounterNotAssignedMessageFormat = new LocalizableResourceString(nameof(Resources.RefCounterNotAssignedMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description1 = new LocalizableResourceString(nameof(Resources.RefCounter001AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
		private static readonly LocalizableString Description2 = new LocalizableResourceString(nameof(Resources.RefCounter002AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
		private static readonly LocalizableString Description3 = new LocalizableResourceString(nameof(Resources.RefCounter003AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
	    private static readonly LocalizableString Description4 = new LocalizableResourceString(nameof(Resources.RefCounter004AnalyzerDescription), Resources.ResourceManager, typeof(Resources));


        private static DiagnosticDescriptor RuleRefCounterError = new DiagnosticDescriptor(RefCounterErrorDiagnosticId, Title, RefCounterErrorMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description1);
		private static DiagnosticDescriptor RuleRefCounterSkip = new DiagnosticDescriptor(RefCounterSkipDiagnosticId, Title, RefCounterSkipMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description2);
	    private static DiagnosticDescriptor RuleRefCounterParseError = new DiagnosticDescriptor(RefCounterParseErrorId, Title, RefCounterParseErroMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description3);
	    private static DiagnosticDescriptor RuleRefCounterNotAssigned = new DiagnosticDescriptor(RefCounterNotAssignedErrorId, Title, RefCounterNotAssignedMessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description3);

        public static readonly LocalizableString SkipReasonAssignedToOtherLocalVars = new LocalizableResourceString(nameof(Resources.SkipReasonAssignedToOtherLocalVars), Resources.ResourceManager, typeof(Resources));
        public static readonly LocalizableString SkipReasonSupportedInitialization = new LocalizableResourceString(nameof(Resources.SkipReasonSupportedInitialization), Resources.ResourceManager, typeof(Resources));
	    public static readonly LocalizableString SkipReasonMoreThanOneAddRef = new LocalizableResourceString(nameof(Resources.SkipReasonMoreThanOneAddRef), Resources.ResourceManager, typeof(Resources));
	    public static readonly LocalizableString SkipReasonMoreThanOneDelRef = new LocalizableResourceString(nameof(Resources.SkipReasonMoreThanOneDelRef), Resources.ResourceManager, typeof(Resources));

	    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
	    {
	        get
	        {
	            return ImmutableArray.Create(
                    RuleRefCounterError, 
                    RuleRefCounterSkip, 
                    RuleRefCounterParseError,
	                RuleRefCounterNotAssigned);
	        }
	    }

	    public override void Initialize(AnalysisContext context)
		{
			// TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
			context.RegisterSyntaxNodeAction(Handle, 
                SyntaxKind.ObjectCreationExpression,
                SyntaxKind.LocalDeclarationStatement,
			    SyntaxKind.Parameter,
                SyntaxKind.InvocationExpression, 
                SyntaxKind.SimpleMemberAccessExpression);
		}

		private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }
            try
            {
                if (context.Node is LocalDeclarationStatementSyntax localDeclaration)
                {
                    HandleLocalDeclaration(context, localDeclaration);
                }

                if (context.Node is ParameterSyntax parameter)
                {
                    HandleMethodParameter(context, parameter);
                }

	            if (context.Node is InvocationExpressionSyntax invocation)
	            {
					HandleInvocation(context, invocation);
				}

                if (context.Node is ObjectCreationExpressionSyntax creation)
                {
                    HandleCreation(context, creation);
                }
            }
            catch (Exception e)
            {
                var diagnostic = Diagnostic.Create(RuleRefCounterParseError, context.Node.GetLocation(), e);
                context.ReportDiagnostic(diagnostic);
            }
        }

		private static void HandleInvocation(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
		{
		    var semanticModle = context.SemanticModel;
		    var canel = context.CancellationToken;
		    var symbolInfo = semanticModle.GetSymbolInfo(invocation.Expression, canel);
		    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
		    {
		        if (RefCounter.IsAssignableTo(methodSymbol.ReturnType))
		        {
		            if (!AssignmentUtils.IsGetMethod(invocation, context.SemanticModel))
		            {
		                ProcessCreation(invocation, context);
		            }
		        }
		    }
		}

	    private static void HandleCreation(SyntaxNodeAnalysisContext context, ObjectCreationExpressionSyntax invocation)
	    {
	        var semanticModle = context.SemanticModel;
	        var canel = context.CancellationToken;
	        var symbolInfo = semanticModle.GetSymbolInfo(invocation.Type, canel);

	        if (symbolInfo.Symbol is ITypeSymbol typeSymbol)
	        {
	            if (RefCounter.IsAssignableTo(typeSymbol))
	            {
	                ProcessCreation(invocation, context);
	            }
	        }
	    }

        private static void ProcessCreation(ExpressionSyntax creationSyntax, SyntaxNodeAnalysisContext context)
	    {
	        var parent = creationSyntax.Parent;
            if (parent is CastExpressionSyntax cast)
	        {
	            ProcessCreation(cast, context);
	        }
	        else
	        {
	            if (!(parent is EqualsValueClauseSyntax) && !(parent is ReturnStatementSyntax) && !(parent is AssignmentExpressionSyntax))
		        {
		            var diagnostic = Diagnostic.Create(RuleRefCounterNotAssigned, creationSyntax.GetLocation(), creationSyntax);
		            context.ReportDiagnostic(diagnostic);
                }
	        }
	    }

	    private static void HandleMethodParameter(SyntaxNodeAnalysisContext context, ParameterSyntax parameter)
	    {
	        var isRefCounter = RefCounter.IsRefCounterType(context.SemanticModel, parameter.Type);
	        if (isRefCounter)
	        {
		        var param = context.SemanticModel.GetDeclaredSymbolSafe(parameter, context.CancellationToken);

				if (param != null)
	            {
	                var method = parameter.FirstAncestorOrSelf<MethodDeclarationSyntax>();
	                var block = method?.Body;
                    if (block == null)
	                {
	                    return;
	                }
					RefCounterStatus status = new RefCounterStatus();

                    ProcessLocalOrParamenterVar(context, param, block, status);
                }
            }
	    }

	    private static void HandleLocalDeclaration(SyntaxNodeAnalysisContext context,
	        LocalDeclarationStatementSyntax localDeclaration)
	    {
			// 根据声明语句声明的Type(可能是var)，获得SymbolInfo

            var isRefCounter = RefCounter.IsRefCounterType(context.SemanticModel, localDeclaration.Declaration.Type);
	        if (isRefCounter)
	        {
	            foreach (var variable in localDeclaration.Declaration.Variables)
	            {
	                HandleLocalVariable(context, variable);
	            }
            }
	    }

		private static void HandleLocalVariable(SyntaxNodeAnalysisContext context, VariableDeclaratorSyntax variable)
	    {
	        if (context.SemanticModel.GetDeclaredSymbolSafe(variable, context.CancellationToken) is
	            ILocalSymbol local)
	        {
	            var block = variable.FirstAncestorOrSelf<BlockSyntax>();
	            if (block == null)
	            {
	                return;
	            }
		        RefCounterStatus status = new RefCounterStatus();

				AssignmentUtils.ProcessVariableInitialization(variable, context.SemanticModel, context.CancellationToken, status);

		        ProcessLocalOrParamenterVar(context, local, block, status);
	        }
	    }

	    private static void ProcessLocalOrParamenterVar(SyntaxNodeAnalysisContext context, ISymbol local,
	        BlockSyntax block, RefCounterStatus status)
	    {
	        RightOfAssignmentUtils.ProcessRightSideOfAssignment(local, block, context.SemanticModel,
	            context.CancellationToken, status);

	        SystemCollectionUtils.ProcessAddedToCollection(local, block, context.SemanticModel, context.CancellationToken,
	            status);
	        LeftOfAssignmentUtils.ProcessLeftSideOfAssignment(local, block, context.SemanticModel,
	            context.CancellationToken, status);

	        IncDelRefMethodUtils.ProcessIncDelRefInvocation(local, block, context.SemanticModel, context.CancellationToken,
	            status);

	        AssignmentUtils.ProcessOutRefInvocation(local, block, context.SemanticModel, context.CancellationToken, status);

	        ReturnUtils.ProcessReturnStatement(local, block, context.SemanticModel, context.CancellationToken, status);

	        ReportError(context, local, status);
	    }

	    private static void ReportError(SyntaxNodeAnalysisContext context, ISymbol local,
		    RefCounterStatus status)
	    {
	        if (!status.IsSkip)
	        {
	            if (status.AssignCounter > 1)
	            {
	                var diagnostic = Diagnostic.Create(RuleRefCounterSkip, local.Locations[0], local.Name,
	                    string.Format("assigned more than once: {0}", status.AssignInfo));
	                context.ReportDiagnostic(diagnostic);
                }
	            else if (status.RefCounter != 0)
	            {

					var diagnostic = Diagnostic.Create(RuleRefCounterError, local.Locations[0], local.Name,
						status.RefCounter, status.RefInfo);
	                context.ReportDiagnostic(diagnostic);
	            }
	        }
	        else
	        {
	            var diagnostic = Diagnostic.Create(RuleRefCounterSkip, local.Locations[0], local.Name,
		            status.SkipReason);
	            context.ReportDiagnostic(diagnostic);
	        }
	    }


		

		


		

	    

		

		
	}
}
