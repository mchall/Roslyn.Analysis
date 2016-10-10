using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Portal.CodeAnalyzer.StandAlone.Modules
{
    public class ReplaceWithVar : BaseSyntaxRewriter
    {
        public ReplaceWithVar(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        { }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            if (!node.Declaration.Type.IsVar && !node.Modifiers.Any(m => m.Kind() == SyntaxKind.ConstKeyword) && node.Declaration.Variables.Count == 1)
            {
                var newDeclaration = TryReplace(node.Declaration);
                return node.WithDeclaration(newDeclaration);
            }
            return base.VisitLocalDeclarationStatement(node);
        }

        public override SyntaxNode VisitUsingStatement(UsingStatementSyntax node)
        {
            if (node.Declaration != null && !node.Declaration.Type.IsVar)
            {
                var newDeclaration = TryReplace(node.Declaration);
                return node.WithDeclaration(newDeclaration);
            }
            return base.VisitUsingStatement(node);
        }

        private VariableDeclarationSyntax TryReplace(VariableDeclarationSyntax declaration)
        {
            var variable = declaration.Variables.First();
            if (variable.Initializer != null)
            {
                var t1 = SemanticModel.GetTypeInfo(variable.Initializer.Value);
                var t2 = SemanticModel.GetTypeInfo(declaration.Type);

                if (t1.Type != null && t2.Type != null && t1.Type.ToString() == t2.Type.ToString())
                {
                    Altered = true;
                    var type = SyntaxFactory.IdentifierName("var")
                        .WithLeadingTrivia(declaration.Type.GetLeadingTrivia())
                        .WithTrailingTrivia(declaration.Type.GetTrailingTrivia());
                    return declaration.WithType(type);
                }
            }
            return declaration;
        }
    }
}