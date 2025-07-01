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

        // Treat as a glob pattern
        var matcher = new Matcher();
        
        // If the pattern doesn't contain wildcards, assume it's a folder and add /**/*.csproj
        if (!folderPathOrPattern.Contains('*') && !folderPathOrPattern.Contains('?'))
        {
            var basePath = folderPathOrPattern;
            if (!basePath.EndsWith(Path.DirectorySeparatorChar))
            {
                basePath += Path.DirectorySeparatorChar;
            }
            matcher.AddInclude($"**/*.csproj");
            
            // Use current directory as base if path doesn't exist as a directory
            var baseDirectory = Directory.Exists(folderPathOrPattern) ? folderPathOrPattern : Directory.GetCurrentDirectory();
            var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(baseDirectory)));
            return result.Files.Select(f => Path.Combine(baseDirectory, f.Path));
        }
        else
        {
            // Handle as a true glob pattern
            matcher.AddInclude(folderPathOrPattern);
            
            // Determine base directory from the pattern
            var baseDirectory = GetBasDirectoryFromPattern(folderPathOrPattern);
            var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(baseDirectory)));
            return result.Files.Select(f => Path.Combine(baseDirectory, f.Path));
        }
    }

    private static string GetBasDirectoryFromPattern(string pattern)
    {
        // Find the first occurrence of wildcards and take the directory part before it
        var wildcardIndex = Math.Min(
            pattern.IndexOf('*') >= 0 ? pattern.IndexOf('*') : int.MaxValue,
            pattern.IndexOf('?') >= 0 ? pattern.IndexOf('?') : int.MaxValue
        );
        
        if (wildcardIndex == int.MaxValue)
        {
            // No wildcards, use the pattern as is if it's a directory
            return Directory.Exists(pattern) ? pattern : Directory.GetCurrentDirectory();
        }
        
        var basePart = pattern.Substring(0, wildcardIndex);
        var lastSeparator = basePart.LastIndexOf(Path.DirectorySeparatorChar);
        
        if (lastSeparator >= 0)
        {
            var baseDir = basePart.Substring(0, lastSeparator);
            return Directory.Exists(baseDir) ? baseDir : Directory.GetCurrentDirectory();
        }
        
        return Directory.GetCurrentDirectory();
    }
}
