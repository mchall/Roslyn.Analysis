using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslyn.CodeAnalyzer.StandAlone.Modules
{
    internal class UnusedDalManagerReport : BaseSyntaxRewriter
    {
        private bool _flag;

        public List<string> UnusedDalManagers = new List<string>();

        public UnusedDalManagerReport(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        { }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var symbol = SemanticModel.GetDeclaredSymbol(node);

            _flag = false;
            if (symbol.Name == "DalManager")
            {
                _flag = true;
            }
            return base.VisitClassDeclaration(node);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (_flag)
            {
                var symbol = SemanticModel.GetDeclaredSymbol(node);
                if (!IsReferenced(symbol))
                {
                    UnusedDalManagers.Add(symbol.Name);
                }
            }
            return base.VisitPropertyDeclaration(node);
        }
    }
}