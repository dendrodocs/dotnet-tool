# Copilot Instructions for DendroDocs.Tool

## Project Overview

DendroDocs.Tool is a .NET command-line tool that analyzes .NET projects and solutions, producing JSON output conforming to the DendroDocs Schema. It's the successor to the Living Documentation Analyzer and uses shared code from the DendroDocs.Shared library.

## Technology Stack

- **Language**: C# (.NET 8.0)
- **Build System**: NUKE build automation
- **Testing Framework**: MSTest
- **Package Manager**: NuGet with central package management
- **CI/CD**: GitHub Actions

## Build and Test

### Build Commands

```bash
# Build the project
./build.sh Compile

# Run tests
./build.sh UnitTests

# Create packages
./build.sh Pack

# Full CI/CD pipeline
./build.sh Push
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Project Structure

```
/src/DendroDocs.Tool/        # Main tool implementation
  /Analyzers/                # Code analyzers
  /Extensions/               # Extension methods
  Program.cs                 # Entry point
  Options.cs                 # Command-line options
  AnalyzerSetup.cs          # Analyzer configuration

/tests/                      # Test projects
  /DendroDocs.Tool.Tests/   # Unit tests
  /AnalyzerSetupVerification/ # Analyzer verification tests

/build/                      # NUKE build scripts
```

## Code Style and Conventions

- Follow the `.editorconfig` rules in the repository root
- Use C# language features appropriate for .NET 8.0
- Write unit tests for new functionality
- Maintain test coverage (current coverage tracked via Coveralls)

## Dependencies

- Uses central package management via `Directory.Packages.props`
- Shared library: DendroDocs.Shared (from dendrodocs/dotnet-shared-lib)
- Schema: the DendroDocs Schema (from dendrodocs/schema)

## Key Files

- `DendroDocs.Tool.sln` - Main solution file
- `Directory.Build.props` - Build configuration
- `Directory.Packages.props` - Central package management
- `global.json` - .NET SDK version pinning
- `gitversion.yaml` - Versioning configuration
- `dendrodocs.runsettings` - Test run settings

## Security

- SBOM (Software Bill of Materials) is generated for releases
- SBOM attestation is provided via GitHub's `attest-sbom` action
- Trivy is used for vulnerability scanning (see `.trivyignore.yaml`)
- CodeQL scanning is enabled

## When Making Changes

1. **Always build before testing**: Run `./build.sh Compile` to ensure code compiles
2. **Run tests**: Use `./build.sh UnitTests` or `dotnet test` to verify changes
3. **Check formatting**: The project uses `.editorconfig` for consistent formatting
4. **Update tests**: Add or update tests for new functionality
5. **Check coverage**: Ensure test coverage doesn't decrease
6. **Follow naming conventions**: Match existing patterns in the codebase
7. **Update documentation**: Update README.md if adding user-facing features

## Common Tasks

### Adding a New Analyzer

1. Create a new analyzer class in `src/DendroDocs.Tool/Analyzers/`
2. Implement the appropriate analyzer interface or base class
3. Register the analyzer in `AnalyzerSetup.cs`
4. Add unit tests in `tests/DendroDocs.Tool.Tests/`
5. Add verification tests in `tests/AnalyzerSetupVerification/`

### Adding New Command-Line Options

1. Update `Options.cs` with new properties
2. Add appropriate attributes for command-line parsing
3. Update the README.md with new option documentation
4. Add tests for the new option handling

### Debugging

The tool can be debugged by:
- Running it directly: `dotnet run --project src/DendroDocs.Tool/DendroDocs.Tool.csproj -- <args>`
- Setting breakpoints in Visual Studio or VS Code
- Using `--verbose` flag for detailed logging

## CI/CD Pipeline

- **Continuous Integration**: Runs on push and PR to main branch
- **Releases**: Automated package publishing on GitHub releases
- **Coverage**: Results reported to Coveralls
- **Artifacts**: Build artifacts and SBOM are uploaded for releases

## Notes for AI Assistants

- The project uses NUKE build system - don't directly invoke MSBuild/dotnet build unless necessary
- Central package management is enabled - add packages to `Directory.Packages.props`, not individual project files
- The tool outputs JSON conforming to a specific schema - maintain schema compatibility
- Shared functionality should come from DendroDocs.Shared library when possible
- Build artifacts go to `Artifacts/` directory (gitignored)
- The `build/_build.csproj` is part of the NUKE build system and should rarely be modified directly
