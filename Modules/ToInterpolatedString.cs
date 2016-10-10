using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslyn.CodeAnalyzer.StandAlone.Modules
{
    public class ToInterpolatedString : BaseSyntaxRewriter
    {
        public ToInterpolatedString(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        { }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (node != null && node.ToString().ToLower().StartsWith("string.format"))
            {
                if (node.ArgumentList.Arguments.Count > 0)
                {
                    if (node.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        var str = node.ArgumentList.Arguments[0].ToString();
                        for (int i = 1; i < node.ArgumentList.Arguments.Count; i++)
                        {
                            var argument = node.ArgumentList.Arguments[i].WithoutLeadingTrivia().WithoutTrailingTrivia().ToString();
                            str = str.Replace("{" + (i - 1) + "}", "{ " + argument + " }");
                        }
                        Altered = true;
                        return SyntaxFactory.IdentifierName("$" + str);
                    }
                }
            }
            return base.VisitInvocationExpression(node);
        }
    }
}