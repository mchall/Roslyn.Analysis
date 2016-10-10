using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Portal.CodeAnalyzer.StandAlone.Modules
{
    public class UnusedLocalVariableRemover : BaseSyntaxRewriter
    {
        public UnusedLocalVariableRemover(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        { }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            if (node.Declaration.Variables.Count == 1)
            {
                var symbol = SemanticModel.GetDeclaredSymbol(node.Declaration.Variables[0]);
                if (!IsReferenced(symbol))
                {
                    Altered = true;
                    return null;
                }
            }
            return base.VisitLocalDeclarationStatement(node);
        }
    }
}