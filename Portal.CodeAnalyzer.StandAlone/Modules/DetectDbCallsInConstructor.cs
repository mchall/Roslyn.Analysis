using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Portal.CodeAnalyzer.StandAlone.Modules
{
    public class DetectDbCallsInConstructor : BaseSyntaxRewriter
    {
        public DetectDbCallsInConstructor(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        { }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var usingStatements = FindChildrenOfType<UsingStatementSyntax>(node.Body);
            DetectScopeCalls(usingStatements);

            AnalyseSyntax(node);

            return base.VisitConstructorDeclaration(node);
        }

        private void AnalyseSyntax(SyntaxNode node)
        {
            foreach (var invocation in FindChildrenOfType<InvocationExpressionSyntax>(node))
            {
                if (IsLazyInstantiated(invocation))
                    continue;

                var memberAccess = FindChildrenOfType<MemberAccessExpressionSyntax>(invocation).FirstOrDefault();
                if (memberAccess != null)
                {
                    var symbol = SemanticModel.GetSymbolInfo(memberAccess);
                    if (symbol.Symbol != null && symbol.Symbol.Locations.Length > 0)
                    {
                        var location = symbol.Symbol.Locations[0];
                        if (location.SourceTree != null)
                        {
                            var root = location.SourceTree.GetRoot();
                            foreach (var method in FindChildrenOfType<MethodDeclarationSyntax>(root))
                            {
                                if (method.Identifier.ValueText == symbol.Symbol.Name)
                                {
                                    var usingStatements = FindChildrenOfType<UsingStatementSyntax>(method);
                                    DetectScopeCalls(usingStatements, memberAccess);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool IsLazyInstantiated(InvocationExpressionSyntax invocation)
        {
            var creationExpression = TryFindParentOfType<ObjectCreationExpressionSyntax>(invocation);
            if (creationExpression != null)
            {
                var typeName = creationExpression.Type.ToString();
                if (typeName.ToLower().Contains("lazy"))
                    return true;
            }
            return false;
        }

        private void DetectScopeCalls(IEnumerable<UsingStatementSyntax> nodes, SyntaxNode parent = null)
        {
            foreach (var node in nodes)
            {
                if (node.Declaration != null && node.Declaration.ToString().Contains("DalScope.Begin"))
                {
                    var scopeVariableName = node.Declaration.Variables[0].Identifier.ValueText;

                    foreach (var invocation in FindChildrenOfType<InvocationExpressionSyntax>(node))
                    {
                        var str = invocation.ToString();
                        if (str.Contains(scopeVariableName) && (str.Contains(".Fetch") || str.Contains(".TryFetch") || str.Contains(".Exists")))
                        {
                            var location = parent != null ? parent.GetLocation() : node.GetLocation();
                            var info = location.GetLineSpan();
                            Console.WriteLine($"{ info.Path }\nat line { info.StartLinePosition.Line }");
                            Console.WriteLine();
                        }
                    }
                }
            }
        }
    }
}