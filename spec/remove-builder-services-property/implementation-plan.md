# Implementation Plan

## Revised Plan (Detailed)

### Changes from Initial Draft
- Explicitly list impacted contracts, builders, extensions, and tests.
- Define a replacement pattern for child builders that avoids `.Services`.
- Add rollout/backout notes and concrete build/test commands.

### Step-by-Step
1. **Inventory and map usage**
	- Confirm every public builder interface exposing `Services`.
	- Enumerate all call sites using `.Services` on Mississippi builders and reservoir builders.
	- Record test patterns that depend on `.Services`.
2. **Update public contracts (breaking)**
	- Remove `IServiceCollection Services` from:
	  - `IMississippiBuilder<TBuilder>`
	  - `IReservoirBuilder`
	  - `IAqueductServerBuilder`
3. **Refactor builder implementations**
	- Replace public `Services` with private field(s) and `ConfigureServices` usage.
	- Ensure `ConfigureOptions` uses `ConfigureServices` to register options.
4. **Refactor child builder creation**
	- Change `ReservoirBuilderExtensions.AddReservoir` to create a builder that wraps `IMississippiClientBuilder` and delegates to `ConfigureServices`.
	- Change `AqueductBuilderExtensions.AddAqueduct` to create a builder that wraps `IMississippiServerBuilder` and delegates to `ConfigureServices`.
5. **Update registration helpers**
	- Update any builder types that call `.Services` internally (ReservoirBuilder, AqueductServerBuilder, MississippiSiloBuilder) to use `ConfigureServices` and private fields.
6. **Update tests and samples**
	- Replace `TestMississippiSiloBuilder.Services` usage with captured ServiceCollection passed through `ConfigureServices` or explicit service collection variables.
	- Ensure tests that assert registrations can still inspect the collection.
7. **Docs and generators (if needed)**
	- Verify no docs/samples/generators reference `.Services` on Mississippi builders.
8. **Verification**
	- Run build + cleanup + tests per repo rules.

### Files/Modules Likely To Change
- Contracts:
  - src/Common.Abstractions/Builders/IMississippiBuilder.cs
  - src/Reservoir.Abstractions/Builders/IReservoirBuilder.cs
  - src/Aqueduct.Abstractions/Builders/IAqueductServerBuilder.cs
- Builder implementations:
  - src/Sdk.Client/Builders/MississippiClientBuilder.cs
  - src/Sdk.Server/Builders/MississippiServerBuilder.cs
  - src/Sdk.Silo/Builders/MississippiSiloBuilder.cs
  - src/Reservoir/Builders/ReservoirBuilder.cs
  - src/Aqueduct/Builders/AqueductServerBuilder.cs
- Builder extensions:
  - src/Reservoir/ReservoirBuilderExtensions.cs
  - src/Aqueduct/AqueductBuilderExtensions.cs
- Tests:
  - tests/EventSourcing.Brooks.L0Tests/EventSourcingRegistrationsTests.cs
  - tests/EventSourcing.Snapshots.L0Tests/SnapshotRegistrationsTests.cs
  - tests/EventSourcing.UxProjections.L0Tests/UxProjectionRegistrationsTests.cs

### API/Contract Changes
- Remove `IServiceCollection Services` from public builder interfaces.
- Introduce builder implementations that do not expose service collection publicly.

### Observability
- No logging/metrics changes expected.

### Test Plan (Concrete)
- Build Mississippi solution: `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1`
- Cleanup Mississippi solution: `pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1`
- Unit tests Mississippi: `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1`
- If Samples touched: `pwsh ./eng/src/agent-scripts/build-sample-solution.ps1` and `pwsh ./eng/src/agent-scripts/clean-up-sample-solution.ps1`

### Rollout / Backout
- No runtime feature flags required.
- Backout by reverting contract changes and dependent refactors.

### Risks + Mitigations
- **Breaking change** for any external consumers using `.Services` → communicate in release notes; user confirmed no backward compatibility needed.
- **Test churn** from service collection assertions → update tests to capture ServiceCollection via ConfigureServices hooks.
- **Hidden call sites** → use repo-wide search for `.Services` on builder types before finalize.
