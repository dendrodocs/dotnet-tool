using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace DendroDocs.Tool.Tests;
 
internal static partial class TestHelper
{
    [GeneratedRegex(@"(\r\n|\r|\n)")]
    private static partial Regex NewlineChars();

    public static IReadOnlyList<TypeDescription> VisitSyntaxTree(string source, params string[] ignoreErrorCodes)
    {
        source.ShouldNotBeNullOrWhiteSpace("without source code there is nothing to test");

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
        diagnostics.Count().ShouldBe(0, "there shoudn't be any compile errors");

        var semanticModel = compilation.GetSemanticModel(syntaxTree, true);

        var types = new List<TypeDescription>();

        var visitor = new SourceAnalyzer(semanticModel, types);
        visitor.Visit(syntaxTree.GetRoot());

        return types;
    }

    public static string UseUnixNewLine(this string value) => value.UseSpecificNewLine("\n");
    public static string UseWindowsNewLine(this string value) => value.UseSpecificNewLine("\r\n");
    public static string UseEnvironmentNewLine(this string value) => value.UseSpecificNewLine(Environment.NewLine);
    public static string UseSpecificNewLine(this string value, string specificNewline) => NewlineChars().Replace(value, specificNewline);
}
