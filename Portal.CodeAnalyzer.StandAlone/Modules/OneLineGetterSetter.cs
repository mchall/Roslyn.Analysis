using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Portal.CodeAnalyzer.StandAlone.Modules
{
    public class OneLineGetterSetter : BaseSyntaxRewriter
    {
        public OneLineGetterSetter(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        { }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            bool hasBody = false;
            foreach (var accessor in node.AccessorList.Accessors)
            {
                if (accessor.Body != null)
                {
                    hasBody = true;
                }
            }

            if (!hasBody)
            {
                node = node.WithIdentifier(node.Identifier.WithTrailingTrivia(SyntaxFactory.Whitespace(" ")));

                var open = SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithTrailingTrivia(SyntaxFactory.Whitespace(" "));
                //var close = SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithTrailingTrivia(SyntaxFactory.EndOfLine(Environment.NewLine));
                node = node.WithAccessorList(SyntaxFactory.AccessorList(open, node.AccessorList.Accessors, node.AccessorList.CloseBraceToken));
                Altered = true;
            }

            return base.VisitPropertyDeclaration(node);
        }

        public override SyntaxNode VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            if (node.Body == null)
            {
                Altered = true;
                node = node.WithoutTrailingTrivia().WithoutLeadingTrivia().WithTrailingTrivia(SyntaxFactory.Whitespace(" "));
            }
            return base.VisitAccessorDeclaration(node);
        }
    }
}