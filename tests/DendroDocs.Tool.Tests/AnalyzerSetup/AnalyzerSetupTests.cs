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

    private static string GetSolutionPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory().AsSpan();

        var path = currentDirectory[..(currentDirectory.IndexOf("tests") + 6)];

        return Path.Combine(path.ToString(), "AnalyzerSetupVerification");
    }
}
