using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Portal.CodeAnalyzer.Common;

namespace Portal.CodeAnalyzer.Database
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DetectDbCallsInConstructorAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "SqlEntities";
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.DetectDbCallsInConstructor), Resources.ResourceManager, typeof(Resources));
        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.DetectDbCallsInConstructor, Description, Description, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(ConstructorCallsCheck, SyntaxKind.ConstructorDeclaration);
        }

        private static void ConstructorCallsCheck(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node as ConstructorDeclarationSyntax;
            if (node != null)
            {
                var usingStatements = AnalyzerHelpers.FindChildrenOfType<UsingStatementSyntax>(node.Body);
                DetectScopeCalls(context, usingStatements);

                foreach (var invocation in AnalyzerHelpers.FindChildrenOfType<InvocationExpressionSyntax>(node))
                {
                    if (IsLazyInstantiated(invocation))
                        continue;

                    var memberAccess = AnalyzerHelpers.FindChildrenOfType<MemberAccessExpressionSyntax>(invocation).FirstOrDefault();
                    if (memberAccess != null)
                    {
                        var symbol = context.SemanticModel.GetSymbolInfo(memberAccess);
                        if (symbol.Symbol != null && symbol.Symbol.Locations.Length > 0)
                        {
                            var location = symbol.Symbol.Locations[0];
                            if (location.SourceTree != null)
                            {
                                var root = location.SourceTree.GetRoot();
                                foreach (var method in AnalyzerHelpers.FindChildrenOfType<MethodDeclarationSyntax>(root))
                                {
                                    if (method.Identifier.ValueText == symbol.Symbol.Name)
                                    {
                                        usingStatements = AnalyzerHelpers.FindChildrenOfType<UsingStatementSyntax>(method);
                                        DetectScopeCalls(context, usingStatements, memberAccess);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool IsLazyInstantiated(InvocationExpressionSyntax invocation)
        {
            var creationExpression = AnalyzerHelpers.TryFindParentOfType<ObjectCreationExpressionSyntax>(invocation);
            if (creationExpression != null)
            {
                var typeName = creationExpression.Type.ToString();
                if (typeName.ToLower().Contains("lazy"))
                    return true;
            }
            return false;
        }

        private static void DetectScopeCalls(SyntaxNodeAnalysisContext context, IEnumerable<UsingStatementSyntax> nodes, SyntaxNode parent = null)
        {
            foreach (var node in nodes)
            {
                if (node.Declaration != null && node.Declaration.ToString().Contains("DalScope.Begin"))
                {
                    var scopeVariableName = node.Declaration.Variables[0].Identifier.ValueText;

                    foreach (var invocation in AnalyzerHelpers.FindChildrenOfType<InvocationExpressionSyntax>(node))
                    {
                        var str = invocation.ToString();
                        if (str.Contains(scopeVariableName) && (str.Contains(".Fetch") || str.Contains(".TryFetch") || str.Contains(".Exists")))
                        {
                            var location = parent != null ? parent.GetLocation() : node.GetLocation();
                            var diagnostic = Diagnostic.Create(Rule, location);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}