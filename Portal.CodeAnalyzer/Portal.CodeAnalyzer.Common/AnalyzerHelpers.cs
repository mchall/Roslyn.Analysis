using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Portal.CodeAnalyzer.Common
{
    public static class AnalyzerHelpers
    {
        public static IEnumerable<T> FindChildrenOfType<T>(SyntaxNode block)
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

        public static T TryFindParentOfType<T>(SyntaxNode node)
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
    }
}