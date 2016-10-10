using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Portal.CodeAnalyzer.Database
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DalManagerAccessAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "SqlEntities";
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.DalManagerAccess), Resources.ResourceManager, typeof(Resources));
        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.DalManagerAccess, Description, Description, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(CheckMethodAccess, SyntaxKind.ConstructorDeclaration, SyntaxKind.MethodDeclaration);
        }

        private static void CheckMethodAccess(SyntaxNodeAnalysisContext context)
        {
            var method = context.Node as BaseMethodDeclarationSyntax;
            if (method != null)
            {
                if (RequiresTokenModification(method, context.SemanticModel))
                {
                    var diagnostic = Diagnostic.Create(Rule, method.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static bool RequiresTokenModification(BaseMethodDeclarationSyntax node, SemanticModel semanticModel)
        {
            var symbol = semanticModel.GetDeclaredSymbol(node);
            if (symbol.DeclaredAccessibility == Accessibility.Public)
            {
                if (!symbol.ReturnsVoid)
                {
                    if (IsDalEntityType(symbol.ReturnType))
                    {
                        return true;
                    }
                }

                foreach (var parameter in node.ParameterList.Parameters)
                {
                    var paramSymbol = semanticModel.GetDeclaredSymbol(parameter);
                    if (IsDalEntityType(paramSymbol.Type))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsDalEntityType(ITypeSymbol type)
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

        private static bool IsDalEntitiesAssembly(IAssemblySymbol assembly)
        {
            if (assembly != null && assembly.Name == "Portal.Data.SqlEntities")
                return true;
            return false;
        }
    }
}