using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslyn.CodeAnalyzer.StandAlone.Modules
{
    internal class PublicDalManagerRemover : BaseSyntaxRewriter
    {
        public PublicDalManagerRemover(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        { }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            SyntaxTokenList tokenList;
            if (RequiresTokenModification(node, out tokenList))
            {
                node = node.WithModifiers(tokenList);
            }
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            SyntaxTokenList tokenList;
            if (RequiresTokenModification(node, out tokenList))
            {
                node = node.WithModifiers(tokenList);
            }
            return base.VisitConstructorDeclaration(node);
        }

        private bool RequiresTokenModification(BaseMethodDeclarationSyntax node, out SyntaxTokenList syntaxTokenList)
        {
            bool alterRequired = false;

            var symbol = SemanticModel.GetDeclaredSymbol(node);
            if (symbol.DeclaredAccessibility == Accessibility.Public)
            {
                if (!symbol.ReturnsVoid)
                {
                    if (IsDalEntityType(symbol.ReturnType))
                    {
                        alterRequired = true;
                    }
                }

                foreach (var parameter in node.ParameterList.Parameters)
                {
                    var paramSymbol = SemanticModel.GetDeclaredSymbol(parameter);
                    if (IsDalEntityType(paramSymbol.Type))
                    {
                        alterRequired = true;
                        break;
                    }
                }
            }

            if (alterRequired)
            {
                Altered = true;

                var tokenList = new List<SyntaxToken>();
                var modifiers = node.Modifiers;
                for (int i = 0; i < modifiers.Count; i++)
                {
                    if (modifiers[i].IsKind(SyntaxKind.PublicKeyword))
                    {
                        var newSyntaxToken = SyntaxFactory.Token(SyntaxKind.InternalKeyword);
                        newSyntaxToken = newSyntaxToken.WithLeadingTrivia(modifiers[i].LeadingTrivia);
                        newSyntaxToken = newSyntaxToken.WithTrailingTrivia(modifiers[i].TrailingTrivia);
                        tokenList.Add(newSyntaxToken);
                    }
                    else
                    {
                        tokenList.Add(modifiers[i]);
                    }
                }
                syntaxTokenList = SyntaxFactory.TokenList(tokenList);
                return true;
            }

            syntaxTokenList = SyntaxFactory.TokenList(new List<SyntaxToken>());
            return false;
        }

        private bool IsDalEntityType(ITypeSymbol type)
        {
            if (IsDalEntitiesAssembly(type.ContainingAssembly) && type.Name.StartsWith("Dal") || type.Name.StartsWith("IDal"))
            {
                return true;
            }

            var namedType = type as INamedTypeSymbol;
            if (namedType != null)
            {
                if (namedType.IsGenericType)
                {
                    foreach (var argument in namedType.TypeArguments)
                    {
                        if (IsDalEntitiesAssembly(argument.ContainingAssembly) && argument.Name.StartsWith("Dal") || argument.Name.StartsWith("IDal"))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool IsDalEntitiesAssembly(IAssemblySymbol assembly)
        {
            if(assembly != null && assembly.Name == "Portal.Data.SqlEntities")
                return true;
            return false;
        }
    }
}