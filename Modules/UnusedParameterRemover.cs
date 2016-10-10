using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslyn.CodeAnalyzer.StandAlone.Modules
{
    public class UnusedParameterRemover : BaseSyntaxRewriter
    {
        public UnusedParameterRemover(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        { }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.ParameterList.Parameters.Count > 0)
            {
                var methodSymbol = SemanticModel.GetDeclaredSymbol(node);
                if (!methodSymbol.IsOverride && !methodSymbol.IsVirtual && !methodSymbol.IsAbstract)
                {
                    List<ParameterSyntax> unusedParams = new List<ParameterSyntax>();

                    foreach (var parameter in node.ParameterList.Parameters)
                    {
                        var symbol = SemanticModel.GetDeclaredSymbol(parameter);
                        if (!IsReferenced(symbol))
                        {
                            unusedParams.Add(parameter);
                        }
                    }

                    if (unusedParams.Count > 0)
                    {
                        foreach (var parameter in unusedParams)
                        {
                            var altered = node.ParameterList.Parameters.Remove(parameter);
                            var alteredList = node.ParameterList.WithParameters(altered);
                            node = node.WithParameterList(alteredList);
                        }

                        Altered = true;
                    }
                }
            }
            return base.VisitMethodDeclaration(node);
        }
    }
}