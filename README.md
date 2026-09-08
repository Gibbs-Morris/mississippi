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

## Design Principles

- **API familiarity first**
  - New/refactored APIs align with primary .NET behaviors and patterns.
  - When design is ambiguous, follow widely used .NET conventions.
  - Orleans-grain APIs match Orleans developer expectations to minimize context switching and strengthen Developer Experience (DX).

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

- .NET SDK 10.0.400 or a later 10.0.4xx patch, as selected by [global.json](global.json). Its Roslyn 5.9 compiler is required to load the source generators.
- PowerShell 7.0 or later (for build scripts)
- Aspire CLI for direct AppHost workflows (`dotnet tool install -g Aspire.Cli`). The AppHosts use the CLI bundle; `dotnet run` can fall back to the SDK-paired CLI through DNX when `aspire` is not on `PATH`.
- JetBrains Rider or other compatible IDE

### Installation

Mississippi packages are published on NuGet under the `Mississippi.*` naming pattern.

Recommended entry points for application developers are the SDK packages:

- `Mississippi.Sdk.Client` - client-side integration package
- `Mississippi.Sdk.Gateway` - gateway/API integration package
- `Mississippi.Sdk.Runtime` - Orleans runtime hosting integration package

Lower-level packages for advanced scenarios are being enabled in stages as the publish workflow is validated. The first package in this rollout is `Mississippi.Common.Abstractions`, followed by focused abstractions and runtime components such as `Mississippi.EventSourcing.*`, `Mississippi.Inlet.*`, and `Mississippi.Reservoir.*`.

Example install command:

```powershell
dotnet add package Mississippi.Sdk.Gateway --prerelease
```

### Building from Source

To build the project from source:

```powershell
# Clone the repository
git clone https://github.com/Gibbs-Morris/mississippi.git
cd mississippi

# Run the local pipeline (build → L0/L1 tests → summaries → cleanup → final build)
pwsh ./go.ps1
```

Common script entry points:

- `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1 [-Configuration Debug|Release]` – build the Mississippi solution.
- `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1 [-Configuration Debug|Release]` – run L0/L1 tests with coverage for Mississippi projects.
- `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1` – execute Stryker.NET mutation testing.
- `pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1` – apply the repository’s ReSharper cleanup and analyzer inspections.

## Samples

The repository includes sample applications demonstrating the framework:

- **Spring** — A full-stack event-sourced application with Orleans silo, ASP.NET API, and Blazor WASM client. Run `pwsh ./run-spring.ps1` to launch. Explore [`samples/Spring/Spring.Domain`](samples/Spring/Spring.Domain) for the domain model.
- **Crescent** — A minimal Aspire AppHost sample for integration testing patterns.

## Testing

### Validate Spring after a change

Use the same entry point locally and in the **L3 Spring E2E (Smoke)** CI check:

```powershell
pwsh ./test-spring.ps1 -Doctor # Check SDK selection and Docker access
pwsh ./test-spring.ps1         # L3 Smoke: build, launch Aspire, browser-test banking, stop
pwsh ./test-spring.ps1 -TestLevel L2 -Suite Full # API and authorization tests; no browser
pwsh ./test-spring.ps1 -TestLevel L3 -Suite Full # All browser journeys, including smoke
```

Prerequisites are PowerShell 7+, the SDK selected by `global.json`, and an accessible Docker daemon running Linux containers.
First use needs network access to NuGet, the Playwright browser CDN, and Microsoft container images.
On Linux hosts that need Chromium OS libraries, use `-InstallBrowserDependencies`; Playwright's installer may require sudo.
`-Doctor` checks SDK and Docker access only; it does not certify browser libraries, image downloads, or application startup.

The command restores locked packages, installs the Aspire CLI version from `Directory.Packages.props` into this checkout,
and builds only the selected Spring test project and its dependencies with warnings as errors.
L3 installs matching Chromium binaries into `artifacts/tools/playwright`; L2 has no browser dependency.
The L3 smoke suite checks that the stylesheet loads and runs the banking journey:
initialize accounts, observe £500, deposit £50, withdraw £25, and observe £525 via SignalR without refreshing.
`-Configuration Debug` is available; Release is the default. Every run builds incrementally so stale binaries cannot silently pass.

Test level and suite are separate choices. `Spring.L2Tests` holds functional API/infrastructure tests;
`Spring.L3Tests` holds browser journeys, with the smoke subset under `Smoke/` and tagged `Category=Smoke`.
`Full` means all tests at the selected level; run both Full commands above to verify both levels.
The L3 Tests workflow runs Smoke on PRs and merge queues, and offers Smoke or Full when dispatched manually.
See [where tests live and when they run](samples/Spring/TESTING.md) before adding another test.

Aspire's testing host allocates test endpoints and owns the application/container lifetime.
Startup has a three-minute cancellation budget, individual test hangs are limited to five minutes, and the test session is limited to fifteen minutes.
Use separate worktrees for simultaneous runs because builds share `bin` and `obj` within one checkout.
The smoke test complements fast unit tests and the existing cleanup, coverage, and full build gates. Mutation testing is an additional quality signal under the [mutation-testing policy](.github/instructions/mutation-testing.instructions.md).

Each run prints `RESULT` and an absolute `SUMMARY` path under a unique `artifacts/spring/` directory.
`summary.json` records status, phase, test level, suite, project, SDK/Aspire versions, passed count, duration, and artifact location.
Exit code 0 with `PASS` requires a completed test run with at least one test and every selected test passing; missing, skipped, and empty results fail.
Doctor success uses `READY` and never claims tests passed.
Diagnostics include TRX, restore/build/test logs, resource log backlogs, and a banking screenshot and Playwright trace when the browser journey starts.
Open `banking.zip` locally with the generated Playwright script's `show-trace` command.
Artifacts are ignored by Git; CI retains them for seven days. Logs and traces can contain local application data, so review them before sharing.

For interactive exploration, use `pwsh ./run-spring.ps1 -LocalAuth On` and stop with Ctrl+C.
The [Aspire CLI](https://aspire.dev/reference/cli/commands/aspire-run/) also supports detached runs;
always pass the Spring AppHost project explicitly in this multi-AppHost repository and stop that same project afterward.
The [Aspire agent setup](https://aspire.dev/get-started/configure-mcp/) can configure runtime logs/traces through MCP in an individual agent environment.
Keep this repository's `AGENTS.md` and its engineering instructions when adding Aspire's optional agent configuration.

The implementation follows [Aspire test lifecycle guidance](https://aspire.dev/testing/manage-app-host/),
[bounded CI testing](https://aspire.dev/testing/testing-in-ci/), and [Playwright traces](https://playwright.dev/dotnet/docs/trace-viewer-intro).
A test-owned AppHost gives local and CI runs the same assertions and cleanup; interactive MCP is a debugging aid, not the pass/fail gate.
The L3 smoke CI check fails normally; repository administrators can add **L3 Spring E2E (Smoke)** to required branch checks after merging.

### Framework quality gates

The framework includes comprehensive testing:

```powershell
# Run unit tests with code coverage
pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1

# Optional mutation testing (Stryker)
pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1
```

Test results and coverage reports are generated in the `.scratchpad/coverage-test-results` directory, and mutation runs write reports under `.scratchpad/mutation-test-results`.

Mutation testing is being adopted gradually. There is no mandatory repository mutation-score threshold, and mutation results are not an ordinary completion criterion. Prioritize correct delivery and meaningful unit-test coverage, add straightforward assertion improvements, and report significant gaps for dedicated follow-up. Avoid significant time or token expenditure chasing survivors unless explicitly requested. See the [mutation-testing policy](.github/instructions/mutation-testing.instructions.md).

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
- The script prints a concise summary (RESULT, COVERAGE, MUTATION_SCORE) that GitHub Copilot can parse easily.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for full details.

## Contributing

Contributions to the Mississippi Framework are welcome. Follow the [PR size and stacked delivery policy](.github/instructions/pr-size-and-stacking.instructions.md): one logical change per PR, targeting 600 changed lines or fewer, with justified exceptions when a larger change is easier to review intact.

Use a feature branch (or a fork for standalone contributions). Plan dependent work as native GitHub stacked PRs using `gh stack` and the [gh-stack skill](https://github.com/github/gh-stack/blob/main/skills/gh-stack/SKILL.md); native stacks require branches in the same repository. Complete each PR's applicable CI/CD, required approvals, and feedback resolution before starting the next layer. Ready layers can remain open and merge together as a stack.

### Build automation

The PowerShell entry points (`build-*.ps1`, `unit-test-*.ps1`, `clean-up-*.ps1`, `mutation-test-mississippi-solution.ps1`, `orchestrate-solutions.ps1`, etc.) are wrappers over a shared module at `eng/src/agent-scripts/RepositoryAutomation.psm1`. The module exposes reusable advanced functions so the same steps can run from CI jobs, local shells, and Pester tests without duplicating logic. See `eng/src/agent-scripts/README.md` for the catalogue of functions and authoring guidance.

## CI / Local pipeline options

The top-level `go.ps1` forwards to `orchestrate-solutions.ps1`. By default it builds both solutions, runs their L0/L1 tests, summarizes Mississippi coverage, applies ReSharper cleanup, and performs a final build with warnings as errors. Mutation testing is opt-in with `-IncludeMutation`; `-SkipCleanup` skips formatting changes during an intermediate validation run.

Usage examples:

```powershell
# Run the local pipeline including cleanup (default)
pwsh ./go.ps1 -Configuration Release

# Include Mississippi mutation tests and the mutation summary
pwsh ./go.ps1 -Configuration Release -IncludeMutation

# Build and test without applying cleanup
pwsh ./go.ps1 -Configuration Release -SkipCleanup
```

L2 integration tests, PowerShell tests, documentation tests, publishing, and service-backed CI checks run separately. See the [script guide and CI mapping](eng/src/agent-scripts/README.md#github-actions-mapping) for their entry points. Run `pwsh ./clean-up.ps1` before final handoff even when an intermediate run uses `-SkipCleanup`.
