using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Components;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;
using static Serilog.Log;

[GitHubActions("Continuous",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0,
    OnPushBranches = ["main"],
    OnPullRequestBranches = ["main"],
    InvokedTargets = [
        nameof(Push)
    ],
    ImportSecrets = [
        nameof(FeedGitHubToken),
        nameof(NuGetApiKey)
    ]
)]
[UnsetVisualStudioEnvironmentVariables]
[DotNetVerbosityMapping]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Push);

    GitHubActions GitHubActions => GitHubActions.Instance;

    string BranchSpec => GitHubActions?.Ref;

    string BuildNumber => GitHubActions?.RunNumber.ToString();

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("The key to push to Nuget")]
    [Secret]
    readonly string NuGetApiKey;

    [Parameter]
    readonly string GitHubUser = GitHubActions.Instance?.RepositoryOwner ?? "DendroDocs";

    [Parameter]
    [Secret]
    readonly string FeedGitHubToken;

    [Solution(GenerateProjects = true)]
    readonly Solution Solution;

    [Required]
    [GitVersion(Framework = "net8.0", NoCache = true, NoFetch = true)]
    readonly GitVersion GitVersion;

    AbsolutePath ArtifactsDirectory => RootDirectory / "Artifacts";

    AbsolutePath TestResultsDirectory => RootDirectory / "TestResults";

    string SemVer;

    bool IsPullRequest => GitHubActions?.IsPullRequest ?? false;

    bool IsTag => BranchSpec is not null && BranchSpec.Contains("refs/tags", StringComparison.OrdinalIgnoreCase);

    Target Clean => _ => _
        .Executes(() =>
        {
            ArtifactsDirectory.CreateOrCleanDirectory();
            TestResultsDirectory.CreateOrCleanDirectory();
        });

    Target CalculateVersion => _ => _
       .Executes(() =>
       {
           SemVer = GitVersion.SemVer;

           if (IsPullRequest)
           {
               Information("Branch spec {BranchSpec} is a pull request. Adding build number {BuildNumber}", BranchSpec, BuildNumber);

               SemVer = string.Join('.', GitVersion.SemVer.Split('.').Take(3).Union([BuildNumber]));
           }

           Information("SemVer = {SemVer}", SemVer);
       });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(_ => _
                .SetProjectFile(Solution)
            );
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .DependsOn(CalculateVersion)
        .Executes(() =>
        {
            ReportSummary(_ => _
                .WhenNotNull(GitVersion, (v, o) => v
                    .AddPair("Version", o.SemVer)
                )
            );

            DotNetBuild(_ => _
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoLogo()
                .EnableNoRestore()
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
            );
        });

    Target UnitTests => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(_ => _
                .SetProjectFile(Solution.tests.DendroDocs_Tool_Tests.Directory)
                .SetConfiguration(Configuration.Debug)
                .EnableNoLogo()
                .EnableNoRestore()
                .SetVerbosity(DotNetVerbosity.quiet)
                .SetProcessArgumentConfigurator(_ => _
                    .Add("-p:TestingPlatformCommandLineArguments=\"" +
                        "--report-trx " +
                        "--report-trx-filename DendroDocs.Shared.trx " +
                        $"--results-directory {TestResultsDirectory} " +
                        $"--settings {RootDirectory / "dendrodocs.runsettings"} " +
                        "--coverage " +
                        "--coverage-output coverage.cobertura.xml " +
                        "--coverage-output-format cobertura" +
                        "\"")
                )
            );
        });

    Target CodeCoverage => _ => _
        .DependsOn(UnitTests)
        .Executes(() =>
        {
            ReportGenerator(_ => _
                .SetProcessToolPath(NuGetToolPathResolver.GetPackageExecutable("ReportGenerator", "ReportGenerator.dll", framework: "net8.0"))
                .SetTargetDirectory(TestResultsDirectory / "reports")
                .AddReports(TestResultsDirectory / "coverage.cobertura.xml")
                .AddReportTypes(
                    ReportTypes.lcov,
                    ReportTypes.HtmlInline_AzurePipelines_Dark
                )
            );

            string link = TestResultsDirectory / "reports" / "index.html";
            Information($"Code coverage report: \x1b]8;;file://{link.Replace('\\', '/')}\x1b\\{link}\x1b]8;;\x1b\\");
        });

    Target Pack => _ => _
        .DependsOn(UnitTests)
        .DependsOn(CodeCoverage)
        .Produces(ArtifactsDirectory / "*.*nupkg")
        .Executes(() =>
        {
            ReportSummary(_ => _
                .WhenNotNull(SemVer, (c, semVer) => c
                    .AddPair("Packed version", semVer))
            );

            DotNetPack(_ => _
                .SetProject(Solution)
                .SetOutputDirectory(ArtifactsDirectory)
                .SetConfiguration(Configuration == Configuration.Debug ? "Debug" : "Release")
                .EnableNoLogo()
                .EnableNoRestore()
                .EnableContinuousIntegrationBuild()
                .SetVersion(SemVer)
            );
        });

    Target Push => _ => _
        .DependsOn(PushNuget)
        .DependsOn(PushGithub);

    Target PushNuget => _ => _
        .DependsOn(Pack)
        .OnlyWhenDynamic(() => !IsLocalBuild && IsTag)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            DotNetNuGetPush(_ => _
                .SetApiKey(NuGetApiKey)
                .SetTargetPath(ArtifactsDirectory / "*.nupkg")
                .EnableSkipDuplicate()
                .SetSource("https://api.nuget.org/v3/index.json")
                .EnableNoSymbols()
            );
        });

    Target PushGithub => _ => _
        .DependsOn(Pack)
        .OnlyWhenDynamic(() => !IsLocalBuild && !IsTag && !IsPullRequest)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            try
            {
                DotNetNuGetAddSource(_ => _
                   .SetName($"GitHub - {GitHubUser}")
                   .SetUsername(GitHubUser)
                   .SetPassword(FeedGitHubToken)
                   .EnableStorePasswordInClearText()
                   .SetSource($"https://nuget.pkg.github.com/{GitHubUser}/index.json")
                );
            }
            catch
            {
                Information("Source already added");
            }

            DotNetNuGetPush(_ => _
                .SetApiKey(FeedGitHubToken)
                .SetTargetPath(ArtifactsDirectory / "*.nupkg")
                .EnableSkipDuplicate()
                .SetSource($"GitHub - {GitHubUser}")
                .EnableNoSymbols()
            );
        });
}
