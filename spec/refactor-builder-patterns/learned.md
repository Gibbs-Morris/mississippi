# Learned

- Verified: `IReservoirFeatureBuilder<TState>` exposes `Done()` returning `IReservoirBuilder`.
	- src/Reservoir.Abstractions/Builders/IReservoirFeatureBuilder.cs
- Verified: `ReservoirFeatureBuilder<TState>` implements `Done()` by returning its parent builder.
	- src/Reservoir/Builders/ReservoirFeatureBuilder.cs
- Verified: Extension methods depend on `Done()` to return to the parent builder.
	- src/Inlet.Client/SignalRConnection/SignalRConnectionRegistrations.cs
	- src/Reservoir.Blazor/BuiltIn/Navigation/NavigationFeatureRegistration.cs
	- src/Reservoir.Blazor/BuiltIn/Lifecycle/LifecycleFeatureRegistration.cs
- Verified: Inlet client generators emit `featureBuilder.Done()` in generated registrations.
	- src/Inlet.Client.Generators/CommandClientRegistrationGenerator.cs
	- src/Inlet.Client.Generators/ProjectionClientRegistrationGenerator.cs
	- src/Inlet.Client.Generators/SagaClientRegistrationGenerator.cs
- Verified: Tests and samples use `Done()` in builder chains.
	- tests/Reservoir.L0Tests/StoreTests.cs
	- tests/Reservoir.L0Tests/ReservoirRegistrationsTests.cs
	- tests/Reservoir.Blazor.L0Tests/StoreComponentTests.cs
	- tests/Inlet.Client.L0Tests/CompositeInletStoreTests.cs
	- samples/Spring/Spring.Client/Features/*.cs
- Verified: A configure-lambda builder pattern exists in the codebase.
	- src/Inlet.Client/InletBlazorRegistrations.cs (Action<InletBlazorSignalRBuilder>)
- Verified: `InletBlazorSignalRBuilder` has an internal `Build()` used by registrations.
	- src/Inlet.Client/InletBlazorSignalRBuilder.cs
	- src/Inlet.Client/InletBlazorRegistrations.cs
- Verified (external evidence): Microsoft builders commonly use configure actions and return the builder.
	- https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.consoleloggerextensions.addconsole
	- https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.resourceutilizationhealthcheckextensions.addresourceutilizationhealthcheck
	- https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.httpclientfactoryservicecollectionextensions.addhttpclient

## Verification targets
- Builder contracts under src/**.Abstractions/Builders.
- Builder implementations under src/**/Builders and registration extension methods.
- Tests and samples that chain builder APIs.
- Generator outputs that embed registration snippets.
