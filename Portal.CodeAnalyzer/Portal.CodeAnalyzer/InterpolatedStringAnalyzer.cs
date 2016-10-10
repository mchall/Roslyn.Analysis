using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Portal.CodeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InterpolatedStringAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Syntax";
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.InterpolatedString), Resources.ResourceManager, typeof(Resources));

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.ToInterpolatedString, Description, Description, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(ToInterpolatedString, SyntaxKind.InvocationExpression);
        }

        private static void ToInterpolatedString(SyntaxNodeAnalysisContext context)
        {
            var invocation = context.Node as InvocationExpressionSyntax;
            if (invocation != null)
            {
                if (invocation != null && invocation.ToString().ToLower().StartsWith("string.format"))
                {
                    if (invocation.ArgumentList.Arguments.Count > 0)
                    {
                        if (invocation.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.StringLiteralExpression))
                        {
                            var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}