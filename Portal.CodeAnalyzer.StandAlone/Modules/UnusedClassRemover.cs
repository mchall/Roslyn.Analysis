using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Portal.CodeAnalyzer.StandAlone.Modules
{
    public class UnusedClassRemover : BaseSyntaxRewriter
    {
        public UnusedClassRemover(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        { }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var symbol = SemanticModel.GetDeclaredSymbol(node);
            if (!symbol.IsStatic)
            {
                if (!IsReferenced(symbol))
                {
                    Altered = true;
                    return null;
                }
            }
            return base.VisitClassDeclaration(node);
        }
    }
}