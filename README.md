# Mississippi Framework

> ⚠️ **EARLY ALPHA - WORK IN PROGRESS**: This framework is currently in early alpha development stage and not yet at version 1.0. APIs may change significantly without notice. Not recommended for production use at this time.

## Vision

Mississippi’s vision is to make event-sourced, CQRS systems feel as straightforward as writing clean domain logic. You model aggregates, commands, events, and projections, and the framework’s source generators scaffold the API surface, DTOs, client actions, and real-time wiring so you don’t drown in plumbing. Under the hood, Orleans executes commands and appends events; projections build read models; and Inlet pushes versioned projection updates over SignalR into a Blazor WebAssembly client. Reservoir provides a Redux-style store so client state stays predictable and easy to test, and Given/When/Then harnesses keep domain rules fast to verify. Storage is pluggable (Cosmos is one provider), so the same patterns can sit on your chosen backend. The result: a full event-driven architecture—event sourcing, CQRS, actors, real-time UI—delivered with the velocity of a simple app.

📚 **[Full Documentation](https://gibbs-morris.github.io/mississippi/)**

## Quick Start — See It Running

The fastest way to experience Mississippi is to run the **Spring** sample:

```powershell
# Clone and run the Spring sample
git clone https://github.com/Gibbs-Morris/mississippi.git
cd mississippi
pwsh ./run-spring.ps1
```

This launches the full stack—Orleans silo, API, and Blazor WASM client—so you can see event sourcing and real-time projections in action.

### Explore the Domain Model

Take a look at [`samples/Spring/Spring.Domain`](samples/Spring/Spring.Domain) to see how aggregates, commands, events, and projections are defined. The source generators turn these concise domain definitions into a complete API and client layer.

## Overview

Mississippi is a sophisticated .NET framework designed to streamline distributed application development. It provides a robust foundation for building scalable, maintainable .NET applications with built-in support for event sourcing, CQRS, distributed computing via Orleans, cloud storage integration, and real-time UI updates.

## Technology Stack

- **.NET 10.0** - Latest .NET runtime with C# 14.0
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

- .NET 10.0 SDK or later
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

- **Spring** — A full-stack event-sourced application with Orleans silo, ASP.NET API, and Blazor WASM client. Run `pwsh ./run-spring.ps1` to launch. Explore [`samples/Spring/Spring.Domain`](samples/Spring/Spring.Domain) for the domain model.
- **Crescent** — A minimal Aspire AppHost sample for integration testing patterns.

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
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Common.Abstractions.L0Tests -SkipMutation

# Tests + coverage + Stryker mutation score
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Common.Abstractions.L0Tests
```

Notes:

- `-TestProject` can be the project name (convention: `<Project>.L0Tests`) or a path to the `.csproj`.
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

### Build automation

The PowerShell entry points (`build-*.ps1`, `unit-test-*.ps1`, `clean-up-*.ps1`, `mutation-test-mississippi-solution.ps1`, `orchestrate-solutions.ps1`, etc.) are wrappers over a shared module at `eng/src/agent-scripts/RepositoryAutomation.psm1`. The module exposes reusable advanced functions so the same steps can run from CI jobs, local shells, and Pester tests without duplicating logic. See `eng/src/agent-scripts/README.md` for the catalogue of functions and authoring guidance.

## CI / Local pipeline options

The repository provides a top-level helper `go.ps1` which forwards to `orchestrate-solutions.ps1`. A new option `-SkipCleanup` is available to run the full build, tests and mutation testing pipeline while skipping the repository cleanup steps (ReSharper/StyleCop automated cleanup). This is useful for CI runs or local checks when you want to avoid formatting/cleanup changes but still validate build and test quality.

Usage examples:

```powershell
# Run full pipeline locally including cleanup (default)
pwsh ./go.ps1 -Configuration Release

# Run full pipeline but skip cleanup (useful for CI or when you don't want the cleanup step to run)
pwsh ./go.ps1 -Configuration Release -SkipCleanup
```

Notes:

- `-SkipCleanup` is a switch forwarded from `go.ps1` to `orchestrate-solutions.ps1` and then to `Invoke-SolutionsPipeline` in the shared module.

- CI workflows can call `./go.ps1 -SkipCleanup` to run build, tests and mutation testing without running the cleanup step.
