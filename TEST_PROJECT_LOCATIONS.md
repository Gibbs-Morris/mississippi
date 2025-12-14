# Test Project Locations

This document describes the organization and location of test projects across different test levels (L0, L1, L2).

## Test Levels Overview

According to the testing strategy defined in `.github/instructions/testing.instructions.md`:

- **L0**: Pure unit tests — in-memory, blazing-fast, no external dependencies
- **L1**: Slightly richer unit tests — may involve SQL, file system, minimal infrastructure
- **L2**: Functional tests against test deployments — with key stubs/mocks

## Test Project Structure

All test projects are located in the `tests/` directory and follow the naming convention:
`<Product>.<Feature>.L{0|1|2}Tests`

### L0 Test Projects (20 projects)

Pure unit tests with no external dependencies:

- `tests/Architecture.L0Tests/`
- `tests/AspNetCore.Orleans.L0Tests/`
- `tests/Core.L0Tests/`
- `tests/Core.Abstractions.L0Tests/`
- `tests/EventSourcing.Aggregates.L0Tests/`
- `tests/EventSourcing.Aggregates.Abstractions.L0Tests/`
- `tests/EventSourcing.Brooks.L0Tests/`
- `tests/EventSourcing.Brooks.Abstractions.L0Tests/`
- `tests/EventSourcing.Brooks.Cosmos.L0Tests/`
- `tests/EventSourcing.Projections.L0Tests/`
- `tests/EventSourcing.Projections.Abstractions.L0Tests/`
- `tests/EventSourcing.Reducers.L0Tests/`
- `tests/EventSourcing.Reducers.Abstractions.L0Tests/`
- `tests/EventSourcing.Serialization.L0Tests/`
- `tests/EventSourcing.Serialization.Abstractions.L0Tests/`
- `tests/EventSourcing.Serialization.Json.L0Tests/`
- `tests/EventSourcing.Snapshots.L0Tests/`
- `tests/EventSourcing.Snapshots.Abstractions.L0Tests/`
- `tests/EventSourcing.Snapshots.Cosmos.L0Tests/`
- `tests/Hosting.L0Tests/`

### L1 Test Projects (20 projects)

Tests with lightweight infrastructure dependencies:

- `tests/Architecture.L1Tests/`
- `tests/AspNetCore.Orleans.L1Tests/`
- `tests/Core.L1Tests/`
- `tests/Core.Abstractions.L1Tests/`
- `tests/EventSourcing.Aggregates.L1Tests/`
- `tests/EventSourcing.Aggregates.Abstractions.L1Tests/`
- `tests/EventSourcing.Brooks.L1Tests/`
- `tests/EventSourcing.Brooks.Abstractions.L1Tests/`
- `tests/EventSourcing.Brooks.Cosmos.L1Tests/`
- `tests/EventSourcing.Projections.L1Tests/`
- `tests/EventSourcing.Projections.Abstractions.L1Tests/`
- `tests/EventSourcing.Reducers.L1Tests/`
- `tests/EventSourcing.Reducers.Abstractions.L1Tests/`
- `tests/EventSourcing.Serialization.L1Tests/`
- `tests/EventSourcing.Serialization.Abstractions.L1Tests/`
- `tests/EventSourcing.Serialization.Json.L1Tests/`
- `tests/EventSourcing.Snapshots.L1Tests/`
- `tests/EventSourcing.Snapshots.Abstractions.L1Tests/`
- `tests/EventSourcing.Snapshots.Cosmos.L1Tests/`
- `tests/Hosting.L1Tests/`

### L2 Test Projects (20 projects)

Functional tests against test deployments:

- `tests/Architecture.L2Tests/`
- `tests/AspNetCore.Orleans.L2Tests/`
- `tests/Core.L2Tests/`
- `tests/Core.Abstractions.L2Tests/`
- `tests/EventSourcing.Aggregates.L2Tests/`
- `tests/EventSourcing.Aggregates.Abstractions.L2Tests/`
- `tests/EventSourcing.Brooks.L2Tests/`
- `tests/EventSourcing.Brooks.Abstractions.L2Tests/`
- `tests/EventSourcing.Brooks.Cosmos.L2Tests/`
- `tests/EventSourcing.Projections.L2Tests/`
- `tests/EventSourcing.Projections.Abstractions.L2Tests/`
- `tests/EventSourcing.Reducers.L2Tests/`
- `tests/EventSourcing.Reducers.Abstractions.L2Tests/`
- `tests/EventSourcing.Serialization.L2Tests/`
- `tests/EventSourcing.Serialization.Abstractions.L2Tests/`
- `tests/EventSourcing.Serialization.Json.L2Tests/`
- `tests/EventSourcing.Snapshots.L2Tests/`
- `tests/EventSourcing.Snapshots.Abstractions.L2Tests/`
- `tests/EventSourcing.Snapshots.Cosmos.L2Tests/`
- `tests/Hosting.L2Tests/`

## Placeholder Tests

Each L1 and L2 test project contains a placeholder test to verify the project is properly configured and running:

```csharp
[Fact]
[Trait("Level", "L1")]  // or "L2" for L2 tests
public void PlaceholderL1TestShouldPass()
{
    Assert.True(true, "L1 test project is running correctly");
}
```

These placeholder tests can be replaced with actual tests as development progresses.

## Running Tests by Level

To run tests for a specific level, use the xUnit trait filter:

```bash
# Run only L0 tests
dotnet test --filter "Level=L0"

# Run only L1 tests
dotnet test --filter "Level=L1"

# Run only L2 tests
dotnet test --filter "Level=L2"

# Run L1 and L2 tests
dotnet test --filter "Level=L1|Level=L2"
```

## Project Setup

All test projects:
1. Reference the appropriate source project from `src/`
2. Automatically include xUnit, Moq, and other test dependencies via `Directory.Build.props`
3. Follow the zero-warnings policy
4. Are included in the `mississippi.slnx` solution file

## Future Test Development

Contributors should:
1. Add new tests to the appropriate level based on their dependencies
2. Use the `[Trait("Level", "L{0|1|2}")]` attribute to mark test level
3. Follow the naming convention: `<Product>.<Feature>.L{0|1|2}Tests`
4. Ensure all tests are deterministic and isolated
5. Aim for 100% coverage on changed code, maintain ≥80% minimum overall

## Validation Scripts

The following scripts validate the test setup:

- `./build.ps1` - Builds all projects including test projects
- `./clean-up.ps1` - Runs ReSharper code cleanup on all projects
- `./go.ps1` - Runs the complete quality pipeline (build, tests, mutation testing)

All scripts are passing with the current test setup.
