namespace DendroDocs.Tool;

public partial class Program
{
    public class Options
    {
        [Option("solution", Required = true, SetName = "solution", HelpText = "The solution to analyze.")]
        public string? SolutionPath { get; set; }

        [Option("project", Required = true, SetName = "project", HelpText = "The project to analyze.")]
        public string? ProjectPath { get; set; }

        [Option("folder", Required = true, SetName = "folder", HelpText = "The folder to search for projects recursively, or a glob pattern to match specific project files (e.g., 'src/**/*.csproj').")]
        public string? FolderPath { get; set; }

        [Option("exclude", Required = false, Separator = ',', HelpText = "Any projects to exclude from analysis.")]
        public IEnumerable<string> ExcludedProjectPaths { get; set; } = [];

        [Option("output", Required = true, HelpText = "The location of the output.")]
        public string? OutputPath { get; set; }

        [Option('v', "verbose", Default = false, HelpText = "Show warnings during compilation.")]
        public bool VerboseOutput { get; set; }

        [Option('p', "pretty", Default = false, HelpText = "Store JSON output in indented formatting.")]
        public bool PrettyPrint { get; set; }

        [Option('q', "quiet", Default = false, HelpText = "Do not output informational messages.")]
        public bool Quiet { get; set; }
    }
}
