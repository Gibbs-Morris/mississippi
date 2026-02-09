# Implementation Plan

## Changes From Initial Draft

- Replace generic builder concept with explicit Mississippi client/server/silo builders plus package-specific builders (Reservoir, Aqueduct).
- Add explicit migration scope for docs/samples/tests based on verified usage.
- Add explicit abstraction-leak cleanup for registrations living in Abstractions projects.

## Detailed Steps

1. Define builder contracts in Abstractions
	- Add `IMississippiClientBuilder`, `IMississippiServerBuilder`, `IMississippiSiloBuilder` contracts in `Common.Abstractions` (UNVERIFIED exact names/placement).
	- Each contract emphasizes `ConfigureServices`/`ConfigureOptions` hooks without exposing raw `IServiceCollection`.
2. Implement builder adapters
	- Implement builder classes in `Sdk.Client`, `Sdk.Server`, and `Sdk.Silo` projects (these are the existing meta-packages for host builders).
	- Provide extension entry points on `HostApplicationBuilder` and `ISiloBuilder` to create Mississippi builders.
3. Reservoir builder-first surface
	- Add `ReservoirBuilder` and `ReservoirFeatureBuilder` to the Reservoir project.
	- Move registrations (`AddReservoir`, `AddReducer`, `AddActionEffect`, `AddFeatureState`, `AddMiddleware`) into builder-based APIs.
	- Remove legacy `IServiceCollection` registrations once builder-first API is in place.
4. Aqueduct builder-first surface
	- Add `AqueductServerBuilder` (ASP.NET) and `AqueductSiloBuilder` wrappers for Orleans.
	- Align `AddAqueduct`/`UseAqueduct` with builder interfaces and consistent options configuration.
5. Inlet/EventSourcing alignment
	- Add Mississippi builder extensions that wrap existing `AddInlet*` and `AddEventSourcing*` registrations.
	- Remove ServiceCollection registration methods after builder-first replacements are available and docs/tests are updated.
6. Fix abstraction leaks
	- Move `InletInProcessRegistrations` and any concrete registration helpers out of Abstractions if required, or convert to pure contract-only helpers.
7. Update tests to builder-first APIs
	- Refactor L0 tests in Reservoir, Inlet, Aqueduct to use the new builders.
	- Add tests for builder entry points and verify fallback behavior (if retained).
8. Update docs and samples
	- Replace `builder.Services.Add*` examples with builder-first usage in Docusaurus pages.
	- Add migration guidance doc (new page) and update sample `Program.cs` registrations.
9. Validate
	- Build + cleanup + unit tests + mutation (Mississippi) per repo scripts.
	- Update docs lint/format if required by existing tooling.

## Expected Files and Modules

- New/updated builder contracts in `src/Common.Abstractions`.
- New builder implementations in `src/Sdk.Client`, `src/Sdk.Server`, `src/Sdk.Silo`.
- Update Reservoir registration files and add builder types in `src/Reservoir`.
- Update Aqueduct registration files and add builder types in `src/Aqueduct` and `src/Aqueduct.Grains`.
- Update Inlet and EventSourcing registration entry points to expose builder surfaces.
- Update tests in `tests/*` and docs in `docs/Docusaurus/docs/**`.
- Update samples in `samples/**/Program.cs` and `samples/**/CrescentFixture.cs`.

## API/Contract Changes

- New public builder interfaces and fluent registration APIs.
- Removal of `IServiceCollection` registration methods (breaking change). Migration guidance required.

## Observability

- No new logging/metrics expected; reuse existing logging in registration components.

## Test Plan

- Build/cleanup: `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1`, `pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1`.
- Unit tests: `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1`.
- Mutation tests: `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1`.

## Rollout Plan

- Update samples and docs first to validate API shape.
- Remove legacy methods after builder replacements and tests/docs are updated.
- Provide migration notes and before/after code snippets.

## Risks and Mitigations

- Risk: Large surface area changes across many packages. Mitigation: keep changes mechanical, update tests immediately, and gate on full build/test/mutation.
- Risk: Abstractions rules violations. Mitigation: keep contracts in Abstractions, implementations in main projects, and review registrations for concrete types.
