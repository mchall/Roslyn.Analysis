using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Portal.CodeAnalyzer.StandAlone.Modules
{
    public class UnusedConstructorRemover : BaseSyntaxRewriter
    {
        public UnusedConstructorRemover(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        { }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (node.ParameterList.Parameters.Count > 0)
            {
                var symbol = SemanticModel.GetDeclaredSymbol(node);
                if (!IsReferenced(symbol))
                {
                    Altered = true;
                    return null;
                }
            }

            return base.VisitConstructorDeclaration(node);
        }
    }
}