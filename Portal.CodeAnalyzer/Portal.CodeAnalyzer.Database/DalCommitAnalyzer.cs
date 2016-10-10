using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Portal.CodeAnalyzer.Common;

namespace Portal.CodeAnalyzer.Database
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DalCommitAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "SqlEntities";
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.DalCommit), Resources.ResourceManager, typeof(Resources));
        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.DalCommit, Description, Description, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(CommitCheck, SyntaxKind.UsingStatement);
        }

        private static void CommitCheck(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node as UsingStatementSyntax;
            if (node != null)
            {
                if (node.Declaration != null && node.Declaration.ToString().Contains("DalScope.Begin"))
                {
                    var scopeVariableName = node.Declaration.Variables[0].Identifier.ValueText;

                    var block = node.Statement as BlockSyntax;
                    if (block != null)
                    {
                        bool isUpdateBlock = false;
                        bool hasScopeCommit = false;
                        foreach (var invocation in AnalyzerHelpers.FindChildrenOfType<InvocationExpressionSyntax>(block))
                        {
                            var str = invocation.ToString();
                            if (str.Contains(".Update(") || str.Contains(".Insert(") || str.Contains(".Delete("))
                            {
                                isUpdateBlock = true;
                            }

                            if (str.Contains(scopeVariableName) && str.Contains(".Commit("))
                            {
                                hasScopeCommit = true;
                            }
                        }

                        if (isUpdateBlock && !hasScopeCommit)
                        {
                            var diagnostic = Diagnostic.Create(Rule, node.GetLocation());
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}