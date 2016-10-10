using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslyn.CodeAnalyzer.StandAlone.Modules
{
    public class UnusedEnumMemberReport : BaseSyntaxRewriter
    {
        public List<string> UnusedEnumMembers = new List<string>();

        public UnusedEnumMemberReport(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        { }

        public override SyntaxNode VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
        {
            var symbol = SemanticModel.GetDeclaredSymbol(node);
            if (!IsReferenced(symbol))
            {
                UnusedEnumMembers.Add(symbol.Type.Name + "." + symbol.Name);
            }
            return base.VisitEnumMemberDeclaration(node);
        }
    }
}