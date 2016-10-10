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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InterpolatedStringCodeFixProvider)), Shared]
    public class InterpolatedStringCodeFixProvider : CodeFixProvider
    {
        private const string title = "Change to Interpolated String";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticIds.ToInterpolatedString); }
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
                    case DiagnosticIds.ToInterpolatedString:
                        {
                            foreach (var node in nodes)
                            {
                                var invovation = node as InvocationExpressionSyntax;
                                if (invovation != null)
                                {
                                    context.RegisterCodeFix(
                                        CodeAction.Create(
                                            title: title,
                                            createChangedDocument: c => ChangeToInterpolatedString(context.Document, invovation, c),
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

        private async Task<Document> ChangeToInterpolatedString(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var str = invocation.ArgumentList.Arguments[0].ToString();
            for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
            {
                var argument = invocation.ArgumentList.Arguments[i].WithoutLeadingTrivia().WithoutTrailingTrivia().ToString();
                str = str.Replace("{" + (i - 1) + "}", "{ " + argument + " }");
            }

            var newRoot = root.ReplaceNode(invocation, SyntaxFactory.IdentifierName("$" + str));

            return document.WithSyntaxRoot(newRoot);
        }
    }
}