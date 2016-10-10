using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Portal.CodeAnalyzer.StandAlone.Modules
{
    public class EmptyStatementRemover : BaseSyntaxRewriter
    {
        public EmptyStatementRemover(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        { }

        public override SyntaxNode VisitEmptyStatement(EmptyStatementSyntax node)
        {
            Altered = true;

            return node.WithSemicolonToken(
                        SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken)
                            .WithLeadingTrivia(node.SemicolonToken.LeadingTrivia)
                            .WithTrailingTrivia(node.SemicolonToken.TrailingTrivia));
        }
    }
}