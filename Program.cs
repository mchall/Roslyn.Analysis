using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.MSBuild;
using Roslyn.CodeAnalyzer.StandAlone.Modules;

namespace Roslyn.CodeAnalyzer.StandAlone
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            DoCodeRefactor();
            Console.Read();
        }

        private static async void DoCodeRefactor()
        {
            var builder = new StringBuilder();
            var workspace = MSBuildWorkspace.Create();
            var solution = await workspace.OpenSolutionAsync(@"C:\Working\MySolution.sln");

            if (solution != null)
            {
                //Constrain project here
                foreach (var project in solution.Projects)//.Where(p => p.Name == "Domain"))
                {
                    var compilation = await project.GetCompilationAsync();

                    foreach (var document in project.Documents)
                    {
                        var root = await document.GetSyntaxRootAsync();
                        var tree = await document.GetSyntaxTreeAsync();

                        var semanticModel = compilation.GetSemanticModel(tree);

                        //Replace module here
                        var remover = new PublicDalManagerRemover(solution, semanticModel);
                        var result = remover.Visit(root);

                        if (remover.Altered)
                        {
                            var alteredDocument = result.ToFullString();
                            File.WriteAllText(document.FilePath, alteredDocument, Encoding.UTF8);
                        }
                    }
                }
            }
            Console.WriteLine();
            Console.WriteLine("Analysis Done");
        }
    }
}