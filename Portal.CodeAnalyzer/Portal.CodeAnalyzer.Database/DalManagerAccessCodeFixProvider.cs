using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Portal.CodeAnalyzer.Database
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DalManagerAccessCodeFixProvider)), Shared]
    public class DalManagerAccessCodeFixProvider : CodeFixProvider
    {
        private const string title = "Fix exposed DAL object";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticIds.DalManagerAccess); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;
                var nodes = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf();

                switch (diagnostic.Id)
                {
                    case DiagnosticIds.DalManagerAccess:
                        {
                            foreach (var node in nodes)
                            {
                                var constructor = node as ConstructorDeclarationSyntax;
                                if (constructor != null)
                                {
                                    context.RegisterCodeFix(
                                        CodeAction.Create(
                                            title: title,
                                            createChangedDocument: c => ChangeMethodAccessiblity(context.Document, constructor, c),
                                            equivalenceKey: title),
                                        diagnostic);
                                    break;
                                }

                                var method = node as MethodDeclarationSyntax;
                                if (method != null)
                                {
                                    context.RegisterCodeFix(
                                        CodeAction.Create(
                                            title: title,
                                            createChangedDocument: c => ChangeMethodAccessiblity(context.Document, method, c),
                                            equivalenceKey: title),
                                        diagnostic);
                                    break;
                                }
                            }
                        }
                        break;
                }
            }
        }

        private async Task<Document> ChangeMethodAccessiblity(Document document, ConstructorDeclarationSyntax method, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var newTokenList = GetModifiedTokenList(method);

            var newRoot = root.ReplaceNode(method, method.WithModifiers(newTokenList));

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> ChangeMethodAccessiblity(Document document, MethodDeclarationSyntax method, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var newTokenList = GetModifiedTokenList(method);

            var newRoot = root.ReplaceNode(method, method.WithModifiers(newTokenList));

            return document.WithSyntaxRoot(newRoot);
        }

        private static SyntaxTokenList GetModifiedTokenList(BaseMethodDeclarationSyntax method)
        {
            var tokenList = new List<SyntaxToken>();
            var modifiers = method.Modifiers;
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
            return SyntaxFactory.TokenList(tokenList);
        }
    }
}