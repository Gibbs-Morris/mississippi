# Mississippi Framework

> ⚠️ **EARLY ALPHA - WORK IN PROGRESS**: This framework is currently in early alpha development stage and not yet at version 1.0. APIs may change significantly without notice. Not recommended for production use at this time.

## Overview
Mississippi is a sophisticated .NET framework designed to streamline distributed application development. It provides a robust foundation for building scalable, maintainable .NET applications with built-in support for mapping, distributed computing, cloud storage integration, and workflow management.

## Technology Stack
- **.NET 9.0** - Latest .NET runtime with C# 13.0
- **Microsoft Orleans** - Framework for building distributed applications
- **Azure Cosmos DB** - Cloud-native NoSQL database integration

## Development Tooling
- **xUnit** - Unit testing framework
- **Stryker** - Mutation testing for validating test quality
- **SonarAnalyzer** - Static code analysis with SonarCloud gates
- **StyleCop** - Code style enforcement
- **GitVersion** - Semantic versioning

## Getting Started
### Prerequisites
- .NET 9.0 SDK or later
- PowerShell 7.0 or later (for build scripts)
- JetBrains Rider or other compatible IDE

### Installation
The Mississippi Framework will be available as a set of NuGet packages. Package names to be confirmed.

### Building from Source
To build the project from source:

```powershell
# Clone the repository
git clone https://github.com/Gibbs-Morris/mississippi.git
cd mississippi

# Run the build script
./build.ps1
```

Build script options:
- `-SkipBuild` - Skip the build step, useful when you just want to run tests
- `-SkipTests` - Skip running tests, useful when you just want to build the code
- `-SkipStryker` - Skip Stryker mutation testing (which can be time-consuming)
- `-Configuration <Debug|Release>` - Build configuration (default is Release)

## Samples
The repository includes sample applications demonstrating the framework:

- **CrescentApiApp** - An ASP.NET Core API application showing web service integration
- **CrescentConsoleApp** - A console application demonstrating non-web usage

## Testing
The framework includes comprehensive testing:

```powershell
# Run tests with code coverage
./build.ps1 -SkipStryker

# Run tests including mutation testing (Stryker)
./build.ps1
```

Test results and coverage reports are generated in the `test-results` directory.

## GitHub Copilot Workspace Preparation

This repository includes a specialized workflow (`.github/workflows/copilot-setup-steps.yml`) that automatically prepares the development environment for GitHub Copilot Workspace and Agents. When Copilot analyzes this repository, it runs this workflow first to ensure all necessary tools and dependencies are available.

### What the Copilot Setup Workflow Does

The `copilot-setup-steps` job automatically:

1. **Detects and sets up the .NET SDK** - Uses the version specified in `global.json` (currently .NET 9.0.301)
2. **Installs Node.js and tools** - Sets up Node.js 24.x (latest LTS) and installs markdownlint-cli for documentation linting
3. **Installs local development tools** - Restores all tools from `.config/dotnet-tools.json` including:
   - Stryker for mutation testing
   - SonarScanner for code analysis
   - GitVersion for semantic versioning
   - ReSharper command-line tools
   - And other development utilities
4. **Restores package dependencies** - Downloads and caches all NuGet packages for both main and sample solutions
5. **Validates the setup** - Runs a lightweight build to ensure everything is properly configured
6. **Caches artifacts** - Stores .NET packages, tools, and GitVersion data for faster subsequent runs
7. **Generates test results** - Provides test output for Copilot to reference when analyzing code issues

### Automatic Triggers

The workflow runs automatically when:
- Key configuration files change (`global.json`, `Directory.*.props`, `.config/dotnet-tools.json`, `*.slnx`)
- The workflow file itself is modified
- Manually triggered via GitHub Actions UI

### Adapting for New Technologies

When adding new technologies to the repository:

1. **New .NET tools**: Add them to `.config/dotnet-tools.json` and they'll be automatically installed
2. **Different .NET version**: Update `global.json` and the workflow will use the new version
3. **Node.js tools**: Add npm/npx tools to the "Install Node.js tools" step in the workflow
4. **Additional package sources**: Add `nuget.config` and update the workflow to include it in cache keys
5. **Container dependencies**: Add Docker services to the workflow's `services:` section
6. **Non-.NET languages**: Add appropriate setup actions (e.g., `setup-python`, `setup-java`) and restore commands

This ensures Copilot Workspace always has a fully prepared environment without manual setup steps.

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for full details.

## Contributing
Contributions to the Mississippi Framework are welcome. Please follow standard GitHub flow:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request