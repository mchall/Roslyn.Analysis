using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslyn.CodeAnalyzer.StandAlone.Modules
{
    public class UnusedMethodRemover : BaseSyntaxRewriter
    {
        private Accessibility _accessibility;

        public UnusedMethodRemover(Accessibility accessibility, Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        {
            _accessibility = accessibility;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var symbol = SemanticModel.GetDeclaredSymbol(node);
            if (symbol.DeclaredAccessibility == _accessibility)
            {
                if (!IsReferenced(symbol))
                {
                    Altered = true;
                    return null;
                }
            }
            return base.VisitMethodDeclaration(node);
        }
    }
}