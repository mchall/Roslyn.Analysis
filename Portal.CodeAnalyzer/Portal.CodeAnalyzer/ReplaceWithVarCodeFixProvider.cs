using System;
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

namespace Portal.CodeAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReplaceWithVarCodeFixProvider)), Shared]
    public class ReplaceWithVarCodeFixProvider : CodeFixProvider
    {
        private const string title = "Replace with var";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticIds.ReplaceWithVar); }
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
                    case DiagnosticIds.ReplaceWithVar:
                        {
                            foreach (var node in nodes)
                            {
                                var localDeclaration = node as LocalDeclarationStatementSyntax;
                                if (localDeclaration != null)
                                {
                                    context.RegisterCodeFix(
                                        CodeAction.Create(
                                            title: title,
                                            createChangedDocument: c => ChangeDeclarationToVar(context.Document, localDeclaration, c),
                                            equivalenceKey: title),
                                        diagnostic);
                                    break;
                                }

                                var usingStatement = node as UsingStatementSyntax;
                                if (usingStatement != null)
                                {
                                    context.RegisterCodeFix(
                                        CodeAction.Create(
                                            title: title,
                                            createChangedDocument: c => ChangeDeclarationToVar(context.Document, usingStatement, c),
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

        private async Task<Document> ChangeDeclarationToVar(Document document, LocalDeclarationStatementSyntax declaration, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var variable = ConvertToVar(declaration.Declaration);

            var newDeclaration = declaration.ReplaceNode(declaration.Declaration, variable);
            var newRoot = root.ReplaceNode(declaration, newDeclaration);

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> ChangeDeclarationToVar(Document document, UsingStatementSyntax declaration, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var variable = ConvertToVar(declaration.Declaration);

            var newDeclaration = declaration.ReplaceNode(declaration.Declaration, variable);
            var newRoot = root.ReplaceNode(declaration, newDeclaration);

            return document.WithSyntaxRoot(newRoot);
        }

        private VariableDeclarationSyntax ConvertToVar(VariableDeclarationSyntax variable)
        {
            var type = SyntaxFactory.IdentifierName("var")
                                    .WithLeadingTrivia(variable.Type.GetLeadingTrivia())
                                    .WithTrailingTrivia(variable.Type.GetTrailingTrivia());
            return variable.WithType(type);
        }
    }
}