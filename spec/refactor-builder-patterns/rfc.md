# RFC: Refactor Builder Patterns

## Problem
Builder chaining uses terminal methods (for example, `Done()`) that do not feel idiomatic in .NET. The user wants the pattern refactored to align with Microsoft .NET builder conventions.

## Goals
- Align builder APIs with common .NET builder patterns used in Microsoft libraries.
- Remove or replace non-idiomatic terminal methods.
- Update consumers (tests, samples) accordingly.
- Keep fluent usage ergonomic and consistent across client/server/silo/feature builders.

## Non-goals
- Add new features unrelated to builder flow.
- Introduce new external dependencies.
- Change behavior of registrations beyond the builder API surface.

## Current State
- `IReservoirFeatureBuilder<TState>` exposes `Done()` to return to the parent builder.
	- src/Reservoir.Abstractions/Builders/IReservoirFeatureBuilder.cs
- `ReservoirFeatureBuilder<TState>` implements `Done()` by returning its parent builder.
	- src/Reservoir/Builders/ReservoirFeatureBuilder.cs
- Extension methods and built-in feature registrations rely on `Done()` to return to `IReservoirBuilder`.
	- src/Inlet.Client/SignalRConnection/SignalRConnectionRegistrations.cs
	- src/Reservoir.Blazor/BuiltIn/Navigation/NavigationFeatureRegistration.cs
	- src/Reservoir.Blazor/BuiltIn/Lifecycle/LifecycleFeatureRegistration.cs
- Client generators emit `featureBuilder.Done()` in generated registrations.
	- src/Inlet.Client.Generators/CommandClientRegistrationGenerator.cs
	- src/Inlet.Client.Generators/ProjectionClientRegistrationGenerator.cs
	- src/Inlet.Client.Generators/SagaClientRegistrationGenerator.cs
- Tests and samples call `Done()` in builder chains.
	- tests/Reservoir.L0Tests/StoreTests.cs
	- tests/Reservoir.L0Tests/ReservoirRegistrationsTests.cs
	- tests/Reservoir.Blazor.L0Tests/StoreComponentTests.cs
	- tests/Inlet.Client.L0Tests/CompositeInletStoreTests.cs
	- samples/Spring/Spring.Client/Features/*.cs
- The codebase already uses configure-lambda patterns in some registrations.
	- src/Inlet.Client/InletBlazorRegistrations.cs

## Proposed Design
- Preferred hypothesis: move to configure-lambda patterns (for example `AddFeature<T>(Action<IReservoirFeatureBuilder<T>> configure)`), and remove the explicit return-to-parent method where practical.
- Alternative if configure-lambda is not feasible everywhere: replace terminal methods with a name aligned to Microsoft patterns (for example, remove `Done()` in favor of returning the same builder and keep nesting shallow).

## Alternatives
- Rename `Done()` to `Build()` or `Finish()` while keeping return-to-parent pattern.
- Keep current pattern and document it more clearly.
- Introduce a separate builder type with `Build()` that materializes a registry (requires larger changes).

## Security
No new security surface expected; public API changes must be validated.

## Observability
No new logging required.

## Compatibility / Migrations
- Public API changes will require downstream updates.
- Update tests and samples to new patterns.
- Update generator outputs to emit the new pattern.

## Risks
- Widespread API changes could introduce breaking compilation changes.
- Inconsistent patterns across builder types if refactor is partial.
