using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Roslyn.CodeAnalyzer.StandAlone.Modules
{
    public abstract class BaseSyntaxRewriter : CSharpSyntaxRewriter
    {
        protected Solution Solution { get; private set; }
        protected SemanticModel SemanticModel { get; private set; }

        public bool Altered { get; protected set; }

        public BaseSyntaxRewriter(Solution solution, SemanticModel semanticModel)
        {
            Solution = solution;
            SemanticModel = semanticModel;
        }

        protected bool IsReferenced(ISymbol symbol)
        {
            var referencesTask = SymbolFinder.FindReferencesAsync(symbol, Solution);
            referencesTask.Wait();

            foreach (var reference in referencesTask.Result)
            {
                if (reference.Locations.Any())
                {
                    return true;
                }
            }
            return false;
        }

        protected T TryFindParentOfType<T>(SyntaxNode node)
            where T : SyntaxNode
        {
            var parent = node.Parent;
            if (parent == null)
                return default(T);
            if (parent is T)
            {
                return parent as T;
            }
            return TryFindParentOfType<T>(parent);
        }

        protected IEnumerable<T> FindChildrenOfType<T>(SyntaxNode block)
            where T : SyntaxNode
        {
            foreach (var child in block.ChildNodes())
            {
                var t = child as T;
                if (t != null)
                {
                    yield return t;
                }

                foreach (var childT in FindChildrenOfType<T>(child))
                {
                    yield return childT;
                }
            }
        }
    }
}