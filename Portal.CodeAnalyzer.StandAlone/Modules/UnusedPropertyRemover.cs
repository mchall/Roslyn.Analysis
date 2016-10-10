using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Portal.CodeAnalyzer.StandAlone.Modules
{
    public class UnusedPropertyRemover : BaseSyntaxRewriter
    {
        public UnusedPropertyRemover(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        { }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var symbol = SemanticModel.GetDeclaredSymbol(node);
            if (!IsReferenced(symbol))
            {
                Altered = true;
                return null;
            }
            return base.VisitPropertyDeclaration(node);
        }
    }
}