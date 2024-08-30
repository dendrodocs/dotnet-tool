using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Runtime.CompilerServices;

namespace DendroDocs.Tool.Tests;
 
internal class TestHelper
{
    public static IReadOnlyList<TypeDescription> VisitSyntaxTree(string source, params string[] ignoreErrorCodes)
    {
        source.Should().NotBeNullOrWhiteSpace("without source code there is nothing to test");

        var syntaxTree = CSharpSyntaxTree.ParseText(source.Trim());
        var compilation = CSharpCompilation.Create("Test")
                                            .WithOptions(
                                                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                                                    .WithAllowUnsafe(true)
                                            )
                                            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                                            .AddReferences(MetadataReference.CreateFromFile(typeof(DynamicAttribute).Assembly.Location))
                                            .AddReferences(MetadataReference.CreateFromFile(typeof(Nullable<>).Assembly.Location))
                                            .AddSyntaxTrees(syntaxTree);

        var diagnostics = compilation.GetDiagnostics().Where(d => !ignoreErrorCodes.Contains(d.Id));
        diagnostics.Should().HaveCount(0, "there shoudn't be any compile errors");

        var semanticModel = compilation.GetSemanticModel(syntaxTree, true);

        var types = new List<TypeDescription>();

        var visitor = new SourceAnalyzer(semanticModel, types);
        visitor.Visit(syntaxTree.GetRoot());

        return types;
    }
}
