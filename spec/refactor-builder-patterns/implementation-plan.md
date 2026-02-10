# Implementation Plan

## Plan updates after verification
- Confirmed that only `IReservoirFeatureBuilder<TState>` has `Done()` and that generators/tests/samples depend on it.
- Added explicit updates for built-in registrations and Inlet client generators.
- Added external evidence of Microsoft builder patterns using configure actions.

## Detailed checklist
1. Update builder contracts (public API changes)
	 - Replace `IReservoirBuilder.AddFeature<TState>()` with `IReservoirBuilder AddFeature<TState>(Action<IReservoirFeatureBuilder<TState>> configure)`.
		 - src/Reservoir.Abstractions/Builders/IReservoirBuilder.cs
	 - Remove `Done()` from `IReservoirFeatureBuilder<TState>`.
		 - src/Reservoir.Abstractions/Builders/IReservoirFeatureBuilder.cs
2. Update builder implementations
	 - Adjust `ReservoirBuilder.AddFeature<TState>` to accept `Action<IReservoirFeatureBuilder<TState>> configure`, invoke it, and return `this`.
		 - src/Reservoir/Builders/ReservoirBuilder.cs
	 - Remove `Done()` from `ReservoirFeatureBuilder<TState>`.
		 - src/Reservoir/Builders/ReservoirFeatureBuilder.cs
3. Update built-in registrations and extensions
	 - Replace `Done()` chaining with configure lambdas.
		 - src/Inlet.Client/SignalRConnection/SignalRConnectionRegistrations.cs
		 - src/Reservoir.Blazor/BuiltIn/Navigation/NavigationFeatureRegistration.cs
		 - src/Reservoir.Blazor/BuiltIn/Lifecycle/LifecycleFeatureRegistration.cs
	 - Update `InletBlazorSignalRBuilder.Build()` to use the new `AddFeature(..., configure)` signature.
		 - src/Inlet.Client/InletBlazorSignalRBuilder.cs
4. Update generators to emit the new pattern
	 - Replace `featureBuilder.Done()` returns with `builder.AddFeature<TState>(featureBuilder => { ... });` style emission.
		 - src/Inlet.Client.Generators/CommandClientRegistrationGenerator.cs
		 - src/Inlet.Client.Generators/ProjectionClientRegistrationGenerator.cs
		 - src/Inlet.Client.Generators/SagaClientRegistrationGenerator.cs
5. Update tests and samples
	 - Replace `.AddFeature<T>().AddReducer(...).Done()` with `AddFeature<T>(feature => feature.AddReducer(...))`.
		 - tests/Reservoir.L0Tests/StoreTests.cs
		 - tests/Reservoir.L0Tests/ReservoirRegistrationsTests.cs
		 - tests/Reservoir.Blazor.L0Tests/StoreComponentTests.cs
		 - tests/Inlet.Client.L0Tests/CompositeInletStoreTests.cs
		 - samples/Spring/Spring.Client/Features/*.cs
6. Update documentation/comments (if any public XML docs mention Done)
	 - Remove references to `Done()` in XML summaries/remarks in affected APIs.
7. Build and test
	 - Build: `pwsh ./eng/src/agent-scripts/build-mississippi-solution.ps1`
	 - Cleanup: `pwsh ./eng/src/agent-scripts/clean-up-mississippi-solution.ps1`
	 - Unit tests: `pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1`
	 - Mutation tests: `pwsh ./eng/src/agent-scripts/mutation-test-mississippi-solution.ps1`
8. Fix any warnings/errors; rerun affected steps until green.

## Modules/files likely to change
- src/Reservoir.Abstractions/Builders/IReservoirBuilder.cs
- src/Reservoir.Abstractions/Builders/IReservoirFeatureBuilder.cs
- src/Reservoir/Builders/ReservoirBuilder.cs
- src/Reservoir/Builders/ReservoirFeatureBuilder.cs
- src/Inlet.Client/SignalRConnection/SignalRConnectionRegistrations.cs
- src/Reservoir.Blazor/BuiltIn/Navigation/NavigationFeatureRegistration.cs
- src/Reservoir.Blazor/BuiltIn/Lifecycle/LifecycleFeatureRegistration.cs
- src/Inlet.Client/InletBlazorSignalRBuilder.cs
- src/Inlet.Client.Generators/CommandClientRegistrationGenerator.cs
- src/Inlet.Client.Generators/ProjectionClientRegistrationGenerator.cs
- src/Inlet.Client.Generators/SagaClientRegistrationGenerator.cs
- tests/Reservoir.L0Tests/StoreTests.cs
- tests/Reservoir.L0Tests/ReservoirRegistrationsTests.cs
- tests/Reservoir.Blazor.L0Tests/StoreComponentTests.cs
- tests/Inlet.Client.L0Tests/CompositeInletStoreTests.cs
- samples/Spring/Spring.Client/Features/*.cs

## API/contract changes
- `IReservoirBuilder.AddFeature<TState>()` signature change to configure-lambda; return type changes to `IReservoirBuilder`.
- `IReservoirFeatureBuilder<TState>.Done()` removed.
- All call sites updated to configure-lambda pattern.

## Observability
- No new logs or metrics required; registration paths remain unchanged.

## Rollout / backout
- Rollout: update all internal usages and samples in this PR; no compatibility shims.
- Backout: revert the API signature changes and usage updates if needed.

## Risks and mitigations
- Risk: broad compile break due to public API changes.
	- Mitigation: update generators, tests, and samples in the same change; build and run unit tests.
- Risk: inconsistent builder usage.
	- Mitigation: update built-in registrations and generator templates to enforce the pattern.
