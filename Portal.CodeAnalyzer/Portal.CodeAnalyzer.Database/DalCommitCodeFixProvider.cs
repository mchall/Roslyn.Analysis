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

namespace Portal.CodeAnalyzer.Database
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DalCommitCodeFixProvider)), Shared]
    public class DalCommitCodeFixProvider : CodeFixProvider
    {
        private const string title = "Ensure updates are committed";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticIds.DalCommit); }
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
                    case DiagnosticIds.DalCommit:
                        {
                            foreach (var node in nodes)
                            {
                                var usingStatement = node as UsingStatementSyntax;
                                if (usingStatement != null)
                                {
                                    context.RegisterCodeFix(
                                        CodeAction.Create(
                                            title: title,
                                            createChangedDocument: c => CreateScopeCommit(context.Document, usingStatement, c),
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

        private async Task<Document> CreateScopeCommit(Document document, UsingStatementSyntax usingStatement, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var scopeVariableName = usingStatement.Declaration.Variables[0].Identifier.ValueText;
            var block = usingStatement.Statement as BlockSyntax;

            var scope = SyntaxFactory.IdentifierName(scopeVariableName);
            var commit = SyntaxFactory.IdentifierName("Commit");
            var commitAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, scope, commit);
            var invokation = SyntaxFactory.InvocationExpression(commitAccess);

            var statement = SyntaxFactory.ExpressionStatement(invokation)
                .WithLeadingTrivia(block.Statements[0].GetLeadingTrivia())
                .WithTrailingTrivia(SyntaxFactory.EndOfLine(Environment.NewLine));

            block = block.AddStatements(statement);
            var newRoot = root.ReplaceNode(usingStatement, usingStatement.WithStatement(block));

            return document.WithSyntaxRoot(newRoot);
        }
    }
}