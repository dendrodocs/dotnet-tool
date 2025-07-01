using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace DendroDocs.Tool;

public sealed class AnalyzerSetup : IDisposable
{
    public IEnumerable<Project> Projects;
    public readonly Workspace Workspace;

    private AnalyzerSetup(AnalyzerManager Manager)
    {
        this.Workspace = Manager.GetWorkspace();
        this.Projects = this.Workspace.CurrentSolution.Projects;
    }

    public void Dispose()
    {
        this.Workspace.Dispose();
    }

    public static AnalyzerSetup BuildSolutionAnalyzer(string solutionFile, IEnumerable<string> excludedProjects = default!)
    {
        var excludedSet = excludedProjects is not null ? new HashSet<string>(excludedProjects, StringComparer.OrdinalIgnoreCase) : [];

        var manager = new AnalyzerManager(solutionFile);
        var analysis = new AnalyzerSetup(manager);

        // Every project in the solution, except unit test projects
        analysis.Projects = analysis.Projects
            .Where(p => !ProjectContainsTestPackageReference(manager, p))
            .Where(p => string.IsNullOrEmpty(p.FilePath) || !excludedSet.Contains(p.FilePath));

        return analysis;
    }

    public static AnalyzerSetup BuildProjectAnalyzer(string projectFile)
    {
        var manager = new AnalyzerManager();
        manager.GetProject(projectFile);

        return new AnalyzerSetup(manager);
    }

    public static AnalyzerSetup BuildFolderAnalyzer(string folderPathOrPattern, IEnumerable<string> excludedProjects = default!)
    {
        var excludedSet = excludedProjects is not null ? new HashSet<string>(excludedProjects, StringComparer.OrdinalIgnoreCase) : [];
        var projectFiles = DiscoverProjectFiles(folderPathOrPattern);

        if (!projectFiles.Any())
        {
            throw new InvalidOperationException($"No project files found in folder or pattern: {folderPathOrPattern}");
        }

        var manager = new AnalyzerManager();
        foreach (var projectFile in projectFiles)
        {
            if (!excludedSet.Contains(projectFile))
            {
                manager.GetProject(projectFile);
            }
        }

        var analysis = new AnalyzerSetup(manager);
        
        // Filter out test projects and excluded projects
        analysis.Projects = analysis.Projects
            .Where(p => !ProjectContainsTestPackageReference(manager, p))
            .Where(p => string.IsNullOrEmpty(p.FilePath) || !excludedSet.Contains(p.FilePath));

        return analysis;
    }

    private static bool ProjectContainsTestPackageReference(AnalyzerManager manager, Project p)
    {
        return manager.Projects.First(mp => p.Id.Id == mp.Value.ProjectGuid).Value.ProjectFile.PackageReferences.Any(pr => pr.Name.Contains("Test", StringComparison.Ordinal));
    }

    private static IEnumerable<string> DiscoverProjectFiles(string folderPathOrPattern)
    {
        // Check if it's a direct path to a folder
        if (Directory.Exists(folderPathOrPattern))
        {
            return Directory.GetFiles(folderPathOrPattern, "*.csproj", SearchOption.AllDirectories);
        }

        // Handle glob patterns
        var matcher = new Matcher();
        
        // If the pattern doesn't contain wildcards, assume it's a folder that doesn't exist
        if (!folderPathOrPattern.Contains('*') && !folderPathOrPattern.Contains('?'))
        {
            throw new DirectoryNotFoundException($"Folder not found: {folderPathOrPattern}");
        }

        // Handle as a glob pattern
        string baseDirectory;
        string pattern;
        
        // Check if pattern is absolute or relative
        if (Path.IsPathRooted(folderPathOrPattern))
        {
            // Absolute path - extract base directory and relative pattern
            var parts = SplitAbsolutePattern(folderPathOrPattern);
            baseDirectory = parts.BaseDirectory;
            pattern = parts.Pattern;
        }
        else
        {
            // Relative path
            baseDirectory = Directory.GetCurrentDirectory();
            pattern = folderPathOrPattern;
        }

        matcher.AddInclude(pattern);
        var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(baseDirectory)));
        return result.Files.Select(f => Path.Combine(baseDirectory, f.Path));
    }

    private static (string BaseDirectory, string Pattern) SplitAbsolutePattern(string absolutePattern)
    {
        // Find the first wildcard
        var wildcardIndex = Math.Min(
            absolutePattern.IndexOf('*') >= 0 ? absolutePattern.IndexOf('*') : int.MaxValue,
            absolutePattern.IndexOf('?') >= 0 ? absolutePattern.IndexOf('?') : int.MaxValue
        );
        
        if (wildcardIndex == int.MaxValue)
        {
            // No wildcards found, treat as directory
            return (absolutePattern, "**/*.csproj");
        }
        
        // Find the last directory separator before the wildcard
        var basePart = absolutePattern.Substring(0, wildcardIndex);
        var lastSeparator = basePart.LastIndexOf(Path.DirectorySeparatorChar);
        
        if (lastSeparator >= 0)
        {
            var baseDir = basePart.Substring(0, lastSeparator);
            var relativePattern = absolutePattern.Substring(lastSeparator + 1);
            
            // Ensure base directory exists
            if (Directory.Exists(baseDir))
            {
                return (baseDir, relativePattern);
            }
        }
        
        // Fallback to current directory
        return (Directory.GetCurrentDirectory(), absolutePattern);
    }
}
