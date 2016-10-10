using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Portal.CodeAnalyzer.StandAlone.Modules
{
    public class ArgumentNullExceptionNameofFixer : BaseSyntaxRewriter
    {
        public ArgumentNullExceptionNameofFixer(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        { }

        public override SyntaxNode VisitThrowStatement(ThrowStatementSyntax node)
        {
            var text = node.ToString();
            if (text.Contains("ArgumentNullException(\"") && !text.Contains("nameof"))
            {
                var expr = node.Expression as ObjectCreationExpressionSyntax;
                if (expr != null)
                {
                    var argument = expr.ArgumentList.Arguments[0];
                    var literal = argument.Expression as LiteralExpressionSyntax;
                    if (literal != null)
                    {
                        var value = literal.Token.ValueText;

                        bool valid = false;
                        var method = TryFindParentOfType<MethodDeclarationSyntax>(node);
                        if (method != null)
                        {
                            foreach (var parameter in method.ParameterList.Parameters)
                            {
                                if (parameter.Identifier.ValueText == value)
                                {
                                    valid = true;
                                    break;
                                }
                            }
                        }

                        if (valid)
                        {
                            Altered = true;

                            argument = SyntaxFactory.Argument(SyntaxFactory.IdentifierName("nameof(" + value + ")"));

                            var emptyArgs = expr.ArgumentList.Arguments.RemoveAt(0);

                            var list = SyntaxFactory.ArgumentList(expr.ArgumentList.OpenParenToken, emptyArgs, expr.ArgumentList.CloseParenToken);
                            list = list.AddArguments(argument);

                            expr = expr.WithArgumentList(list);
                            node = node.WithExpression(expr);
                        }
                    }
                }
            }
            return base.VisitThrowStatement(node);
        }
    }
}