using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Roslyn.CodeAnalyzer.StandAlone.Modules
{
    public class EnsureUtf8 : BaseSyntaxRewriter
    {
        public EnsureUtf8(Solution solution, SemanticModel semanticModel)
            : base(solution, semanticModel)
        {
            Altered = true;
        }
    }
}