namespace DendroDocs.Tool.Tests;

[TestClass]
public class AnalyzerSetupTests
{
    private static readonly string SolutionPath = GetSolutionPath();

    [TestMethod]
    public void SolutionShouldLoadAllProjects()
    {
        // Arrange
        var solutionFile = Path.Combine(SolutionPath, "SolutionWithoutTests.sln");

        // Act
        using var analyzerSetup = AnalyzerSetup.BuildSolutionAnalyzer(solutionFile);

        // Assert
        analyzerSetup.Projects.Count().ShouldBe(3);

        var projects = analyzerSetup.Projects.ToList();
        projects.ShouldSatisfyAllConditions(
            () => projects[0].FilePath.ShouldEndWith("Project.csproj"),
            () => projects[1].FilePath.ShouldEndWith("OtherProject.csproj"),
            () => projects[2].FilePath.ShouldEndWith("AnotherProject.csproj")
        );
    }

    [TestMethod]
    public void SolutionShouldFilterTestProjects()
    {
        // Arrange
        var solutionFile = Path.Combine(SolutionPath, "SolutionWithTests.sln");

        // Act
        using var analyzerSetup = AnalyzerSetup.BuildSolutionAnalyzer(solutionFile);

        // Assert
        analyzerSetup.Projects.Count().ShouldBe(3);

        var projects = analyzerSetup.Projects.ToList();
        projects.ShouldSatisfyAllConditions(
            () => projects[0].FilePath.ShouldEndWith("Project.csproj"),
            () => projects[1].FilePath.ShouldEndWith("OtherProject.csproj"),
            () => projects[2].FilePath.ShouldEndWith("AnotherProject.csproj")
        );
    }

    [TestMethod]
    public void SolutionShouldFilterExcludedProject()
    {
        // Arrange
        var solutionFile = Path.Combine(SolutionPath, "SolutionWithoutTests.sln");
        var excludeProjectFile = Path.Combine(SolutionPath, "OtherProject", "OtherProject.csproj");

        // Act
        using var analyzerSetup = AnalyzerSetup.BuildSolutionAnalyzer(solutionFile, [excludeProjectFile]);

        // Assert
        analyzerSetup.Projects.Count().ShouldBe(2);

        var projects = analyzerSetup.Projects.ToList();
        projects.ShouldSatisfyAllConditions(
            () => projects[0].FilePath.ShouldEndWith("Project.csproj"),
            () => projects[1].FilePath.ShouldEndWith("AnotherProject.csproj")
        );
    }

    [TestMethod]
    public void SolutionShouldFilterExcludedProjects()
    {
        // Arrange
        var solutionFile = Path.Combine(SolutionPath, "SolutionWithoutTests.sln");
        var excludeProjectFile1 = Path.Combine(SolutionPath, "OtherProject", "OtherProject.csproj");
        var excludeProjectFile2 = Path.Combine(SolutionPath, "AnotherProject", "AnotherProject.csproj");

        // Act
        using var analyzerSetup = AnalyzerSetup.BuildSolutionAnalyzer(solutionFile, [excludeProjectFile1, excludeProjectFile2]);

        // Assert
        analyzerSetup.Projects.ShouldHaveSingleItem();
        analyzerSetup.Projects.ShouldAllBe(p => p.FilePath != null && p.FilePath.EndsWith("Project.csproj"));
    }

    [TestMethod]
    public void SolutionShouldLoadProject()
    {
        // Arrange
        var projectFile = Path.Combine(SolutionPath, "Project", "Project.csproj");

        // Act
        using var analyzerSetup = AnalyzerSetup.BuildProjectAnalyzer(projectFile);

        // Assert
        analyzerSetup.Projects.ShouldHaveSingleItem();
        analyzerSetup.Projects.ShouldAllBe(p => p.FilePath != null && p.FilePath.EndsWith("Project.csproj"));
    }

    [TestMethod]
    public void FolderShouldLoadAllProjects()
    {
        // Arrange
        var folderPath = SolutionPath;

        // Act
        using var analyzerSetup = AnalyzerSetup.BuildFolderAnalyzer(folderPath);

        // Assert
        analyzerSetup.Projects.Count().ShouldBe(3);

        var projectPaths = analyzerSetup.Projects.Select(p => p.FilePath).ToList();
        projectPaths.ShouldContain(path => path != null && path.EndsWith("Project.csproj"));
        projectPaths.ShouldContain(path => path != null && path.EndsWith("OtherProject.csproj"));
        projectPaths.ShouldContain(path => path != null && path.EndsWith("AnotherProject.csproj"));
    }

    [TestMethod]
    public void FolderShouldFilterTestProjects()
    {
        // Arrange
        var basePath = GetBasePath();
        var folderPath = Path.Combine(basePath, "AnalyzerSetupVerification");

        // Act
        using var analyzerSetup = AnalyzerSetup.BuildFolderAnalyzer(folderPath);

        // Assert
        // Should have 3 projects (excluding TestProject which has test packages)
        analyzerSetup.Projects.Count().ShouldBe(3);

        var projects = analyzerSetup.Projects.ToList();
        projects.ShouldAllBe(p => p.FilePath != null && !p.FilePath.Contains("TestProject"));
    }

    [TestMethod]
    public void FolderShouldFilterExcludedProjects()
    {
        // Arrange
        var folderPath = SolutionPath;
        var excludeProjectFile1 = Path.Combine(SolutionPath, "OtherProject", "OtherProject.csproj");
        var excludeProjectFile2 = Path.Combine(SolutionPath, "AnotherProject", "AnotherProject.csproj");

        // Act
        using var analyzerSetup = AnalyzerSetup.BuildFolderAnalyzer(folderPath, [excludeProjectFile1, excludeProjectFile2]);

        // Assert
        analyzerSetup.Projects.ShouldHaveSingleItem();
        analyzerSetup.Projects.ShouldAllBe(p => p.FilePath != null && p.FilePath.EndsWith("Project.csproj"));
    }

    [TestMethod]
    public void GlobPatternShouldFindMatchingProjects()
    {
        // Arrange
        var basePath = GetBasePath();
        var originalDir = Directory.GetCurrentDirectory();

        try
        {
            // Change to the test base directory to make relative patterns work
            Directory.SetCurrentDirectory(basePath);
            var globPattern = "AnalyzerSetupVerification/**/*.csproj";

            // Act
            using var analyzerSetup = AnalyzerSetup.BuildFolderAnalyzer(globPattern);

            // Assert
            // Should find the 3 non-test projects that match the pattern (excludes TestProject due to test references)
            analyzerSetup.Projects.Count().ShouldBe(3);
            
            var projectPaths = analyzerSetup.Projects.Select(p => p.FilePath).ToList();
            projectPaths.ShouldContain(path => path != null && path.EndsWith("Project.csproj"));
            projectPaths.ShouldContain(path => path != null && path.EndsWith("OtherProject.csproj"));
            projectPaths.ShouldContain(path => path != null && path.EndsWith("AnotherProject.csproj"));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    private static string GetSolutionPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory().AsSpan();

        var path = currentDirectory[..(currentDirectory.IndexOf("tests") + 6)];

        return Path.Combine(path.ToString(), "AnalyzerSetupVerification");
    }

    private static string GetBasePath()
    {
        var currentDirectory = Directory.GetCurrentDirectory().AsSpan();

        return currentDirectory[..(currentDirectory.IndexOf("tests") + 6)].ToString();
    }
}
