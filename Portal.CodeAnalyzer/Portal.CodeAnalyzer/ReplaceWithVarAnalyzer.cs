using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Portal.CodeAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReplaceWithVarAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Syntax";
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ReplaceWithVar), Resources.ResourceManager, typeof(Resources));
        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.ReplaceWithVar, Description, Description, Category, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);
        
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(ReplaceWithVar, SyntaxKind.LocalDeclarationStatement, SyntaxKind.UsingStatement);
        }

        private static void ReplaceWithVar(SyntaxNodeAnalysisContext context)
        {
            var localDeclaration = context.Node as LocalDeclarationStatementSyntax;
            if (localDeclaration != null)
            {
                if (!localDeclaration.Declaration.Type.IsVar && !localDeclaration.Modifiers.Any(m => m.Kind() == SyntaxKind.ConstKeyword) && localDeclaration.Declaration.Variables.Count == 1)
                {
                    if (TypeSafetyCheck(context.SemanticModel, localDeclaration.Declaration))
                    {
                        var diagnostic = Diagnostic.Create(Rule, localDeclaration.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }

            var usingStatement = context.Node as UsingStatementSyntax;
            if (usingStatement != null)
            {
                if (usingStatement.Declaration != null && !usingStatement.Declaration.Type.IsVar)
                {
                    if (TypeSafetyCheck(context.SemanticModel, usingStatement.Declaration))
                    {
                        var diagnostic = Diagnostic.Create(Rule, usingStatement.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static bool TypeSafetyCheck(SemanticModel semanticModel, VariableDeclarationSyntax declaration)
        {
            var variable = declaration.Variables.First();
            if (variable.Initializer != null)
            {
                var t1 = semanticModel.GetTypeInfo(variable.Initializer.Value);
                var t2 = semanticModel.GetTypeInfo(declaration.Type);

                if (t1.Type != null && t2.Type != null && t1.Type.ToString() == t2.Type.ToString())
                {
                    return true;
                }
            }
            return false;
        }
    }
}