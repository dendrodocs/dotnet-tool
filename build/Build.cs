using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.ReportGenerator;
using static Nuke.Common.Tools.Docker.DockerTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Git.GitTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;
using static Serilog.Log;

[UnsetVisualStudioEnvironmentVariables]
[DotNetVerbosityMapping]
class Build : NukeBuild
{
    public const string ProjectName = ProductName + ".Shared";
    public const string ProductName = "DendroDocs";
    public const string RepositoryOwner = "dendrodocs";
    public const string SbomNamespace = "https://sbom.dendrodocs.dev";

    enum BuildFlows
    {
        Local,
        PrRemote, // PR from fork (remote)
        PrLocal,  // PR from same repo/branch
        Push,     // Push to main
        Release   // Tag/release
    }

    // Entrypoint for Nuke CLI
    public static int Main() => Execute<Build>(x => x.Push);

    GitHubActions GitHubActions => GitHubActions.Instance;

    // Pipeline state
    string GitHubRef => GitHubActions?.Ref;
    string GitHubEventName => GitHubActions?.EventName;
    string GitHubRepository => GitHubActions?.Repository;
    string GitHubHeadRepository => Environment.GetEnvironmentVariable("GITHUB_HEAD_REPOSITORY");
    string BuildNumber => GitHubActions?.RunNumber.ToString();

    BuildFlows BuildFlow => GetBuildFlow();

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("The key to push to Nuget")]
    [Secret]
    readonly string NuGetApiKey;

    [Parameter]
    readonly string GitHubUser = GitHubActions.Instance?.RepositoryOwner ?? RepositoryOwner;

    [Parameter]
    [Secret]
    readonly string FeedGitHubToken;

    [Solution(GenerateProjects = true)]
    readonly Solution Solution;

    [Required]
    [GitVersion(Framework = "net8.0", NoCache = true, NoFetch = true)]
    readonly GitVersion GitVersion;

    [NuGetPackage(
        packageId: "Microsoft.Sbom.DotNetTool",
        packageExecutable: "Microsoft.Sbom.DotNetTool.dll",
        Framework = "net8.0")]
    readonly Tool Sbom;

    AbsolutePath ArtifactsDirectory => RootDirectory / "Artifacts";
    AbsolutePath TestResultsDirectory => RootDirectory / "TestResults";
    AbsolutePath TrivyCacheDirectory => RootDirectory / ".trivy-cache";
    AbsolutePath SbomDirectory => RootDirectory / "Sbom";

    string SemVer;

    bool IsPullRequest => GitHubActions?.IsPullRequest ?? false;
    bool IsTag => GitHubRef is not null && GitHubRef.Contains("refs/tags", StringComparison.OrdinalIgnoreCase);

    // Clean ensures output dirs are reset, for reproducible builds
    Target Clean => _ => _
        .Executes(() =>
        {
            ArtifactsDirectory.CreateOrCleanDirectory();
            TestResultsDirectory.CreateOrCleanDirectory();
            SbomDirectory.CreateOrCleanDirectory();
            TrivyCacheDirectory.CreateDirectory();
        });

    // CI safety: only build from a clean git state (prevents accidental, local-only changes from leaking into artifacts)
    Target VerifyCleanGit => _ => _
        .OnlyWhenStatic(() => !IsLocalBuild)
        .Executes(() =>
        {
            var output = Git("status --porcelain")
                .Select(x => x.Text)
                .ToList();

            if (output.Count > 0)
                throw new Exception("Repository is not clean. Commit or stash changes before running CI.");
        });

    // Calculate semantic version from git state, with PR build suffix if applicable
    Target CalculateVersion => _ => _
       .Executes(() =>
       {
           SemVer = GitVersion.SemVer;

           if (IsPullRequest)
           {
               Information("Branch spec {GitHubRef} is a pull request. Adding build number {BuildNumber}", GitHubRef, BuildNumber);

               SemVer = string.Join('.', GitVersion.SemVer.Split('.').Take(3).Union([BuildNumber]));
           }

           Information("SemVer = {SemVer}", SemVer);
       });

    // Restore all NuGet dependencies in locked mode (reproducibility, and prevents random upgrades)
    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(_ => _
                .SetProjectFile(Solution)
                .EnableLockedMode()
            );
        });

    // Build the code—locked to version and CI config
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

    // Dependency hygiene: check for known vulnerabilities, outdated, and deprecated packages
    Target Audit => _ => _
        .Executes(() =>
        {
            DotNet("list package --vulnerable");
            DotNet("list package --outdated");
            DotNet("list package --deprecated");
        });

    // Generate an SBOM for all artifacts using Microsoft's sbom-tool
    Target SbomDeliverable => _ => _
        // Only run SBOM creation for main branch and releases, not PRs
        .OnlyWhenDynamic(() => BuildFlow == BuildFlows.Push || BuildFlow == BuildFlows.Release)
        .DependsOn(Pack)
        .Executes(() =>
        {
            var sbomArgs = new[]
            {
                "generate",
                "-b", ArtifactsDirectory.ToString(), // Base path for the build output
                "-bc", RootDirectory.ToString(),     // Project root
                "-m", SbomDirectory.ToString(),      // Output SBOM manifest dir
                "-pn", ProjectName,                  // Package name"
                "-pv", SemVer,
                "-nsb", SbomNamespace,
                "-ps", ProductName,
                "-li", "true",                       // Enable license info
                "-pm", "true"                        // Enable package manifest
            };

            Sbom(arguments: string.Join(" ", sbomArgs));
        });

    // Run Trivy via Docker, scanning the *source tree* for vulnerabilities, secrets, and misconfigurations
    Target ScanSource => _ => _
        .DependsOn(SbomDeliverable)
        .Executes(() =>
        {
            var skipTrivy = false;

            try
            {
                DockerInfo(s => s
                    .SetProcessExitHandler(p =>
                    {
                        if (p.ExitCode != 0 && IsLocalBuild)
                        {
                            skipTrivy = true;
                        }
                    }));
            }
            catch (ArgumentException ex) when (ex.Message.Contains("docker"))
            {
                skipTrivy = true;
            }

            if (skipTrivy)
            {
                Warning("Docker is not available or not running.");
                Warning("Trivy scan is skipped for the local build.");
                Warning("Implications:");
                Warning("- Vulnerability, secret, and license scanning will not run.");
                Warning("- Your build may NOT be secure or compliant!");
                return;
            }

            DockerRun(s => s
                // Trivy version 0.64.1 linux/amd64 corresponds to this SHA256 hash
                .SetImage("aquasec/trivy@sha256:a22415a38938a56c379387a8163fcb0ce38b10ace73e593475d3658d578b2436")
                .SetRm(true)
                .AddVolume($"{RootDirectory}:/src:ro")
                .AddVolume($"{TrivyCacheDirectory}:/root/.cache/trivy:rw")
                .SetCommand("fs")
                .SetArgs(
                    "--ignore-unfixed",
                    "--scanners", "vuln,secret,misconfig",
                    "--disable-telemetry",
                    "--no-progress",
                    "--skip-dirs", ".nuke/temp",
                    "--exit-code", "1",
                    "--ignorefile", "/src/.trivyignore.yaml",
                    "--show-suppressed",
                    "/src")

                    .SetProcessExitHandler(p =>
                    {
                        if (p.ExitCode == 1)
                        {
                            Assert.Fail("Trivy scan failed. This indicates vulnerabilities, secrets, or misconfigurations in the source code.");
                        }
                    }));
        });

    // Run tests, outputting full logs and code coverage results
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
                .AddProcessAdditionalArguments(
                    "-p:TestingPlatformCommandLineArguments=\"" +
                    "--report-trx " +
                    "--report-trx-filename DendroDocs.Shared.trx " +
                    $"--results-directory {TestResultsDirectory} " +
                    $"--settings {RootDirectory / "dendrodocs.runsettings"} " +
                    "--coverage " +
                    "--coverage-output coverage.cobertura.xml " +
                    "--coverage-output-format cobertura" +
                    "\""
                )
            );
        });

    // Generate human-readable and machine-parseable coverage reports for downstream analysis
    Target CodeCoverage => _ => _
        .DependsOn(UnitTests)
        .Executes(() =>
        {
            ReportGenerator(_ => _
                .SetTargetDirectory(TestResultsDirectory / "reports")
                .AddReports(TestResultsDirectory / "coverage.cobertura.xml")
                .AddReportTypes(
                    ReportTypes.lcov,
                    ReportTypes.HtmlInline_AzurePipelines_Dark
                )
                .AddFileFilters("-*.g.cs")
            );

            string link = TestResultsDirectory / "reports" / "index.html";
            Information($"Code coverage report: \x1b]8;;file://{link.Replace('\\', '/')}\x1b\\{link}\x1b]8;;\x1b\\");
        });

    // Pack and hash all artifacts, including .nupkg and .snupkg, and output both individual and aggregate SHA256 files.
    Target Pack => _ => _
        .DependsOn(UnitTests)
        .DependsOn(CodeCoverage)
        .DependsOn(Audit)
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

            // Hash all produced .nupkg and .snupkg files for downstream integrity and attestation
            var hashFileBuilder = new StringBuilder();

            string[] extensions = ["*.nupkg", "*.snupkg"];
            var packages = extensions.SelectMany(ext => Directory.GetFiles(ArtifactsDirectory, ext));
            foreach (var package in packages)
            {
                var relativePath = Path.GetFileName(package);

                var hash = SHA256.HashData(File.ReadAllBytes(package));
                var hashString = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                File.WriteAllText(package + ".sha256", hashString);
                hashFileBuilder.AppendLine($"{hashString}  {relativePath}");
                Information($"SHA256 for {package}: {hashString}");
            }

            // Write aggregate hash file (SHA256SUMS) for attestation and consumer verification
            File.WriteAllText(ArtifactsDirectory / "SHA256SUMS", hashFileBuilder.ToString());
        });

    // Chain all push steps
    Target Push => _ => _
        .DependsOn(PushNuget)
        .DependsOn(PushGithub);

    // This target brings together source scanning and SBOM production as the 'proof' step and verify clean state before releasing
    Target Proof => _ => _
        .DependsOn(VerifyCleanGit)
        .DependsOn(ScanSource)
        .DependsOn(SbomDeliverable);

    // Push to NuGet.org with precondition checks and integrity verification
    Target PushNuget => _ => _
        .DependsOn(Proof)
        .OnlyWhenDynamic(() => BuildFlow == BuildFlows.Release)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            VerifyPackageHashes();

            DotNetNuGetPush(_ => _
                .SetApiKey(NuGetApiKey)
                .SetTargetPath(ArtifactsDirectory / "*.nupkg")
                .EnableSkipDuplicate()
                .SetSource("https://api.nuget.org/v3/index.json")
                .EnableNoSymbols()
            );
        });

    // Push to GitHub Packages, after all proof steps and only from trusted context
    Target PushGithub => _ => _
        .DependsOn(Proof)
        .OnlyWhenDynamic(() => BuildFlow == BuildFlows.Push)
        .OnlyWhenDynamic(() => GitHubUser == RepositoryOwner)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            VerifyPackageHashes();

            DotNetNuGetPush(_ => _
                .SetApiKey(FeedGitHubToken)
                .SetTargetPath(ArtifactsDirectory / "*.nupkg")
                .EnableSkipDuplicate()
                .SetSource($"https://nuget.pkg.github.com/{RepositoryOwner}/index.json")
                .EnableNoSymbols()
            );
        });

    BuildFlows GetBuildFlow()
    {
        if (IsLocalBuild)
        {
            return BuildFlows.Local;
        }

        // PR event
        if (GitHubEventName?.StartsWith("pull_request") == true)
        {
            // Forked repo PRs (external contributors)
            if (!string.IsNullOrWhiteSpace(GitHubHeadRepository) && !string.Equals(GitHubHeadRepository, GitHubRepository, StringComparison.OrdinalIgnoreCase))
            {
                return BuildFlows.PrRemote;
            }

            // In-repo branch PRs
            return BuildFlows.PrLocal;
        }

        // Tag/release
        if (IsTag || GitHubEventName == "release")
        {
            return BuildFlows.Release;
        }

        // Branch push
        if (GitHubRef?.StartsWith("refs/heads/") == true)
        {
            return BuildFlows.Push;
        }

        return BuildFlows.Local;
    }

    // Always verify artifact hashes before publishing—no accidental or malicious tampering
    void VerifyPackageHashes()
    {
        var packages = Directory.GetFiles(ArtifactsDirectory, "*.nupkg");
        foreach (var package in packages)
        {
            var hashFile = package + ".sha256";

            if (!File.Exists(hashFile))
            {
                throw new Exception($"Missing SHA256 file for {package}");
            }

            var computedHash = SHA256.HashData(File.ReadAllBytes(package));
            var expectedHash = File.ReadAllText(hashFile).Trim();
            var actualHash = BitConverter.ToString(computedHash).Replace("-", string.Empty).ToLowerInvariant();

            if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"SHA256 mismatch for {package}: expected {expectedHash}, got {actualHash}");
            }
        }
    }
}
