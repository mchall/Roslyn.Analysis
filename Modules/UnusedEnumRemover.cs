using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslyn.CodeAnalyzer.StandAlone.Modules
{
    public class UnusedEnumRemover : BaseSyntaxRewriter
    {
        public UnusedEnumRemover(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        { }

        public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var symbol = SemanticModel.GetDeclaredSymbol(node);
            if (!IsReferenced(symbol))
            {
                Altered = true;
                return null;
            }
            return base.VisitEnumDeclaration(node);
        }
    }
}