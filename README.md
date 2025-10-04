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

# Run the full pipeline (build → test → mutation → cleanup)
pwsh ./go.ps1
```

Common script entry points:

- `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1 [-Configuration Debug|Release]` – build the Mississippi solution.
- `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1 [-Configuration Debug|Release]` – run unit/integration tests with coverage for Mississippi projects.
- `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1` – execute Stryker.NET mutation testing.
- `pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1` – apply the repository’s ReSharper cleanup and analyzer inspections.

## Samples

The repository includes sample applications demonstrating the framework:

- **CrescentApiApp** - An ASP.NET Core API application showing web service integration
- **CrescentConsoleApp** - A console application demonstrating non-web usage

## Testing

The framework includes comprehensive testing:

```powershell
# Run unit tests with code coverage
pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1

# Run mutation testing (Stryker)
pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1
```

Test results and coverage reports are generated in the `.scratchpad/coverage-test-results` directory, and mutation runs write reports under `.scratchpad/mutation-test-results`.

For a fast loop on a single test project, use the helper script:

```powershell
# Tests + coverage only (fast)
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests -SkipMutation

# Tests + coverage + Stryker mutation score
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Core.Abstractions.Tests
```

Notes:

- `-TestProject` can be the project name (convention: one test project per assembly) or a path to the `.csproj`.
- If the source project can’t be inferred from `<ProjectReference>`, set `-SourceProject` to the target `.csproj`.
- The script prints a concise summary (RESULT, COVERAGE, MUTATION_SCORE) that tools like Cursor or Copilot can parse easily.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for full details.

## Contributing

Contributions to the Mississippi Framework are welcome. Please follow standard GitHub flow:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request
