using Newtonsoft.Json;
using System.Diagnostics;

namespace DendroDocs.Tool;

public static partial class Program
{
    private static ParserResult<Options>? ParsedResults;

    public static Options RuntimeOptions { get; private set; } = new Options();

    public static async Task<int> Main(string[] args)
    {
        ParsedResults = Parser.Default.ParseArguments<Options>(args);

        var resultCode = await ParsedResults.MapResult(
            options => RunApplicationAsync(options),
            errors => Task.FromResult(1)
        );

        return resultCode;
    }

    private static async Task<int> RunApplicationAsync(Options options)
    {
        RuntimeOptions = options;

        var types = new List<TypeDescription>();

        var stopwatch = Stopwatch.StartNew();

        using (var analyzer = options.SolutionPath is not null
            ? AnalyzerSetup.BuildSolutionAnalyzer(options.SolutionPath, options.ExcludedProjectPaths)
            : AnalyzerSetup.BuildProjectAnalyzer(options.ProjectPath!))
        {
            await AnalyzeWorkspace(types, analyzer).ConfigureAwait(false);
        }

        stopwatch.Stop();

        // Write analysis 
        var serializerSettings = JsonDefaults.SerializerSettings();
        serializerSettings.Formatting = options.PrettyPrint ? Formatting.Indented : Formatting.None;

        var result = JsonConvert.SerializeObject(types.OrderBy(t => t.FullName), serializerSettings);
        File.WriteAllText(options.OutputPath!, result);

        if (!options.Quiet)
        {
            Console.WriteLine($"DendroDocs Analysis output generated in {stopwatch.ElapsedMilliseconds}ms at {options.OutputPath}");
            Console.WriteLine($"- {types.Count} types found");
        }

        // Validate the JSON output
        if (JsonValidator.ValidateJson(ref result, out var validationErrors))
        {
            if (!options.Quiet)
            {
                Console.WriteLine("- JSON is valid according to the schema.");
            }
        }
        else
        {
            if (!options.Quiet)
            {
                Console.WriteLine();
            }
            
            Console.Error.WriteLine("Generated JSON file is invalid. See errors below:");
            foreach (var error in validationErrors)
            {
                Console.Error.WriteLine($"- {error}");
            }

            return 1;
        }

        return 0;
    }

    private static async Task AnalyzeWorkspace(List<TypeDescription> types, AnalyzerSetup analysis)
    {
        foreach (var project in analysis.Projects)
        {
            await AnalyzeProjectAsyc(types, project).ConfigureAwait(false);
        }
    }

    private static async Task AnalyzeProjectAsyc(List<TypeDescription> types, Project project)
    {
        var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
        if (compilation is null)
        {
            return;
        }

        if (RuntimeOptions.VerboseOutput)
        {
            var diagnostics = compilation.GetDiagnostics();
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                Console.WriteLine($"The following errors occured during compilation of project '{project.FilePath}'");
                foreach (var diagnostic in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                {
                    Console.WriteLine($"- {diagnostic}");
                }
            }
        }

        // Every file in the project
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree, true);

            var visitor = new SourceAnalyzer(semanticModel, types);
            visitor.Visit(syntaxTree.GetRoot());
        }
    }
}
