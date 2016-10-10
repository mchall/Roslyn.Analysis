using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Portal.CodeAnalyzer.StandAlone.Modules
{
    public class DetectDbCallsInLoop : BaseSyntaxRewriter
    {
        public DetectDbCallsInLoop(Solution solution, SemanticModel semanticModel)
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
                    foreach (var statement in block.Statements)
                    {
                        var foreachStatement = statement as ForEachStatementSyntax;
                        if (foreachStatement != null)
                        {
                            DetectInLoopBlock(foreachStatement.Statement, scopeVariableName);
                        }

                        var forStatement = statement as ForStatementSyntax;
                        if (forStatement != null)
                        {
                            DetectInLoopBlock(forStatement.Statement, scopeVariableName);
                        }

                        var whileStatement = statement as WhileStatementSyntax;
                        if (whileStatement != null)
                        {
                            DetectInLoopBlock(whileStatement.Statement, scopeVariableName);
                        }

                        var doStatement = statement as DoStatementSyntax;
                        if (doStatement != null)
                        {
                            DetectInLoopBlock(doStatement.Statement, scopeVariableName);
                        }
                    }
                }
            }

            return base.VisitUsingStatement(node);
        }

        private void DetectInLoopBlock(StatementSyntax statement, string scopeVariableName)
        {
            foreach (var invocation in FindChildrenOfType<InvocationExpressionSyntax>(statement))
            {
                var str = invocation.ToString();
                if (str.Contains(scopeVariableName) && (str.Contains(".Fetch") || str.Contains(".TryFetch") || str.Contains(".Exists")))
                {
                    var info = statement.GetLocation().GetLineSpan();
                    Console.WriteLine($"{ info.Path }\nat line { info.StartLinePosition.Line }");
                    Console.WriteLine();
                    break;
                }
            }
        }
    }
}