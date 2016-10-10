using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslyn.CodeAnalyzer.StandAlone.Modules
{
    public class DalCommitCheck : BaseSyntaxRewriter
    {
        public DalCommitCheck(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        { }

        public override SyntaxNode VisitUsingStatement(UsingStatementSyntax node)
        {
            if (node.Declaration != null && node.Declaration.ToString().Contains("DalScope.Begin"))
            {
                var scopeVariableName = node.Declaration.Variables[0].Identifier.ValueText;

                var block = node.Statement as BlockSyntax;
                if (block != null)
                {
                    bool isUpdateBlock = false;
                    bool hasScopeCommit = false;
                    foreach (var statement in FindChildrenOfType<InvocationExpressionSyntax>(block))
                    {
                        var str = statement.ToString();
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
                        Altered = true;

                        var scope = SyntaxFactory.IdentifierName(scopeVariableName);
                        var commit = SyntaxFactory.IdentifierName("Commit");
                        var commitAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, scope, commit);
                        var invokation = SyntaxFactory.InvocationExpression(commitAccess);

                        var statement = SyntaxFactory.ExpressionStatement(invokation)
                            .WithLeadingTrivia(block.Statements[0].GetLeadingTrivia())
                            .WithTrailingTrivia(SyntaxFactory.EndOfLine(Environment.NewLine));

                        block = block.AddStatements(statement);
                        node = node.WithStatement(block);
                    }
                }
            }

            return base.VisitUsingStatement(node);
        }
    }
}