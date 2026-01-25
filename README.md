# Mississippi Framework

> ⚠️ **EARLY ALPHA - WORK IN PROGRESS**: This framework is currently in early alpha development stage and not yet at version 1.0. APIs may change significantly without notice. Not recommended for production use at this time.

## Overview

Mississippi is an **event sourcing framework** for .NET that moves domain changes from Orleans grains into live Blazor WebAssembly UX state—in real time.

If you're building a CQRS/ES application where users need to see changes the moment they happen (collaborative editing, dashboards, notifications), Mississippi provides the full vertical stack from command handling to browser state updates.

📖 **[Full Documentation](docs/Docusaurus/docs/index.md)** | 🏗️ **[Architecture](docs/Docusaurus/docs/concepts/architecture.md)** | 🚀 **[Getting Started](docs/Docusaurus/docs/getting-started/index.md)**

## Core Concepts

| Concept | Description |
| --- | --- |
| **Aggregates** | Command handling grains that validate business rules and emit events |
| **Brooks** | Append-only event streams stored in Azure Cosmos DB |
| **UX Projections** | Read models that subscribe to events and build query-optimized views |
| **Snapshots** | Point-in-time state captures for fast projection rebuilds |
| **Aqueduct** | Orleans-backed SignalR backplane for multi-server delivery |
| **Inlet** | Client bridge managing subscriptions and real-time updates |
| **Reservoir** | Redux-style state container for Blazor WebAssembly |
| **Refraction** | Holographic HUD component library for Blazor |

## Technology Stack

- **.NET 10.0** — Latest .NET runtime with C# 14.0
- **Microsoft Orleans** — Framework for building distributed applications
- **Azure Cosmos DB** — Cloud-native NoSQL database for event and snapshot storage
- **SignalR** — Real-time client-server communication
- **Blazor WebAssembly** — Client-side web UI framework

## Development Tooling

- **xUnit** — Unit testing framework
- **Stryker** — Mutation testing for validating test quality
- **SonarAnalyzer** — Static code analysis with SonarCloud gates
- **StyleCop** — Code style enforcement
- **GitVersion** — Semantic versioning

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
- PowerShell 7.0 or later (for build scripts)
- Azure Cosmos DB account (or emulator for local development)
- Azure Storage account (or Azurite for local development)

### SDK Packages

Install the appropriate SDK package for your project type:

```bash
# Blazor WebAssembly client
dotnet add package Mississippi.Sdk.Client

# ASP.NET API server
dotnet add package Mississippi.Sdk.Server

# Orleans silo host
dotnet add package Mississippi.Sdk.Silo
```

### Building from Source

```powershell
# Clone the repository
git clone https://github.com/Gibbs-Morris/mississippi.git
cd mississippi

# Run the full pipeline (build → test → mutation → cleanup)
pwsh ./go.ps1
```

Common script entry points:

- `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1` — Build the Mississippi solution
- `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1` — Run unit/integration tests with coverage
- `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1` — Execute Stryker.NET mutation testing
- `pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1` — Apply ReSharper cleanup and analyzer inspections

## Samples

The repository includes sample applications demonstrating the framework:

| Sample | Description |
| --- | --- |
| **[Spring](samples/Spring/)** | Full-stack reference implementation with aggregate, projections, and real-time Blazor client |
| **[Crescent](samples/Crescent/)** | Integration test harness with Aspire AppHost |

### Spring Sample Structure

```text
samples/Spring/
├── Spring.AppHost/     # Aspire orchestration
├── Spring.Client/      # Blazor WebAssembly app with Reservoir
├── Spring.Domain/      # Domain types, commands, events
├── Spring.Server/      # ASP.NET API with SignalR hub
├── Spring.Silo/        # Orleans silo host
└── Spring.L2Tests/     # Integration tests
```

## Testing

The framework includes comprehensive testing with coverage and mutation analysis:

```powershell
# Run unit tests with code coverage
pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1

# Run mutation testing (Stryker)
pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1

# Fast loop on a single test project
pwsh ./eng/src/agent-scripts/test-project-quality.ps1 -TestProject Aqueduct.L0Tests -SkipMutation
```

Test results and coverage reports are generated in `.scratchpad/coverage-test-results/`, and mutation runs write reports under `.scratchpad/mutation-test-results/`.

## CI / Local Pipeline Options

The repository provides a top-level helper `go.ps1` for the full pipeline:

```powershell
# Run full pipeline locally including cleanup (default)
pwsh ./go.ps1 -Configuration Release

# Skip cleanup step (useful for CI or when you don't want formatting changes)
pwsh ./go.ps1 -Configuration Release -SkipCleanup
```

## Documentation

- **[Start Here](docs/Docusaurus/docs/index.md)** — Framework overview and conceptual building blocks
- **[Concepts](docs/Docusaurus/docs/concepts/index.md)** — Architecture and design patterns
- **[Getting Started](docs/Docusaurus/docs/getting-started/index.md)** — Installation and first project
- **[Components](docs/Docusaurus/docs/platform/index.md)** — Component-by-component reference
- **[Contributing](docs/Docusaurus/docs/contributing/index.md)** — Contribution guidelines

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for full details.

## Contributing

Contributions to the Mississippi Framework are welcome. Please follow standard GitHub flow:

1. Fork the repository
2. Create a feature branch
3. Make your changes following the [contribution guidelines](docs/Docusaurus/docs/contributing/index.md)
4. Submit a pull request

The PowerShell entry points are wrappers over a shared module at `eng/src/agent-scripts/RepositoryAutomation.psm1`. See [eng/src/agent-scripts/README.md](eng/src/agent-scripts/README.md) for the catalogue of functions and authoring guidance.
