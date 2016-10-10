using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslyn.CodeAnalyzer.StandAlone.Modules
{
    public class DuplicateUsingRemover : BaseSyntaxRewriter
    {
        private HashSet<string> _usingTrack;

        public DuplicateUsingRemover(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        {
            _usingTrack = new HashSet<string>();
        }

        public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
        {
            var text = node.ToString().Trim();
            if (_usingTrack.Add(text))
                return base.VisitUsingDirective(node);
            Altered = true;
            return null;
        }
    }
}