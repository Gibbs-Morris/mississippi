# Verification

## Claim list
1. Builder APIs with terminal methods like `Done()` are inconsistent with Microsoft .NET builder conventions.
2. A configure-lambda pattern can replace terminal methods without losing expressiveness.
3. All existing builder usage sites can be migrated with minimal behavioral changes.
4. Tests and samples can be updated to the new pattern without weakening coverage.
5. Generator outputs can be updated to emit the new pattern.
6. The new pattern will remain consistent across client/server/silo/feature builder types.

## Verification questions
1. Which builder interfaces currently expose terminal methods like `Done()` (or equivalents), and where are they defined?
2. Which builder implementations rely on terminal methods to return to parent builders?
3. What extension methods use nested fluent patterns that depend on `Done()`?
4. Which tests and samples call `Done()` or equivalent return-to-parent methods?
5. Which code generators emit builder chaining that includes terminal methods?
6. Are there existing configure-lambda patterns in the codebase for builder sub-configuration?
7. Do any builders currently expose `Build()` methods that materialize a final object?
8. Which public API docs or XML comments describe the terminal methods explicitly?
9. What package consumers (samples) will be broken by removing terminal methods?
10. What is the minimal change set to switch to a configure-lambda pattern for feature builders?
11. Are there builder patterns in Microsoft .NET libraries that clearly show preferred structure (evidence source)?
12. Does any internal test harness or helper builder depend on a `Done()`-style method?

## Verification answers
1. `IReservoirFeatureBuilder<TState>` exposes `Done()` returning `IReservoirBuilder`.
	- src/Reservoir.Abstractions/Builders/IReservoirFeatureBuilder.cs
2. `ReservoirFeatureBuilder<TState>` implements `Done()` by returning its parent builder.
	- src/Reservoir/Builders/ReservoirFeatureBuilder.cs
3. Extension methods that depend on `Done()` include:
	- `SignalRConnectionRegistrations.AddSignalRConnectionFeature` (returns `featureBuilder.Done()`).
	  - src/Inlet.Client/SignalRConnection/SignalRConnectionRegistrations.cs
	- `NavigationFeatureRegistration.AddBuiltInNavigation` (chain ends with `.Done()`).
	  - src/Reservoir.Blazor/BuiltIn/Navigation/NavigationFeatureRegistration.cs
	- `LifecycleFeatureRegistration.AddBuiltInLifecycle` (chain ends with `.Done()`).
	  - src/Reservoir.Blazor/BuiltIn/Lifecycle/LifecycleFeatureRegistration.cs
4. Tests using `Done()` include:
	- tests/Reservoir.L0Tests/StoreTests.cs
	- tests/Reservoir.L0Tests/ReservoirRegistrationsTests.cs
	- tests/Reservoir.Blazor.L0Tests/StoreComponentTests.cs
	- tests/Inlet.Client.L0Tests/CompositeInletStoreTests.cs
5. Inlet client generators emit `featureBuilder.Done()`:
	- src/Inlet.Client.Generators/CommandClientRegistrationGenerator.cs
	- src/Inlet.Client.Generators/ProjectionClientRegistrationGenerator.cs
	- src/Inlet.Client.Generators/SagaClientRegistrationGenerator.cs
6. Configure-lambda patterns already exist:
	- `AddInletBlazorSignalR` accepts `Action<InletBlazorSignalRBuilder>` and invokes it before building.
	  - src/Inlet.Client/InletBlazorRegistrations.cs
	- Common builder contracts already accept `Action<IServiceCollection>` and `Action<TOptions>`.
	  - src/Common.Abstractions/Builders/IMississippiBuilder.cs
7. The only builder-style `Build()` in this area is `InletBlazorSignalRBuilder.Build()` (internal), invoked by registrations.
	- src/Inlet.Client/InletBlazorSignalRBuilder.cs
	- src/Inlet.Client/InletBlazorRegistrations.cs
	No public builder interfaces in the Mississippi builders expose `Build()`.
8. `IReservoirFeatureBuilder<TState>.Done()` has XML docs explicitly describing returning to the parent builder.
	- src/Reservoir.Abstractions/Builders/IReservoirFeatureBuilder.cs
9. Samples using `Done()` and therefore breaking on removal include Spring client feature registrations:
	- samples/Spring/Spring.Client/Features/ProjectionsFeatureRegistration.cs
	- samples/Spring/Spring.Client/Features/DualEntitySelection/DualEntitySelectionFeatureRegistration.cs
	- samples/Spring/Spring.Client/Features/DemoAccounts/DemoAccountsFeatureRegistration.cs
10. Minimal change set for a configure-lambda pattern:
	 - Replace `IReservoirBuilder.AddFeature<TState>()` (returning `IReservoirFeatureBuilder<TState>`) with
		a configure-lambda signature such as `IReservoirBuilder AddFeature<TState>(Action<IReservoirFeatureBuilder<TState>> configure)`.
	 - Remove `IReservoirFeatureBuilder<TState>.Done()` and adjust `ReservoirFeatureBuilder<TState>` accordingly.
	 - Update call sites (extension methods, generators, tests, samples) to pass lambdas instead of chaining `Done()`.
	 - Update generator templates to emit the new pattern.
11. Microsoft builder patterns emphasize configure actions and returning the builder:
	 - `ILoggingBuilder AddConsole(Action<ConsoleLoggerOptions>)` returns the builder.
		https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.consoleloggerextensions.addconsole
	 - `IHealthChecksBuilder AddResourceUtilizationHealthCheck(Action<ResourceUtilizationHealthCheckOptions>)` returns the builder.
		https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.resourceutilizationhealthcheckextensions.addresourceutilizationhealthcheck
	 - `IServiceCollection AddHttpClient(Action<HttpClient>)` returns an `IHttpClientBuilder` for further configuration.
		https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.httpclientfactoryservicecollectionextensions.addhttpclient
12. Helper builders in tests (for example `TestMississippiClientBuilder`) do not expose `Done()`; the usage is in tests and
	 feature registration helpers rather than builder test doubles.
	 - tests/Reservoir.L0Tests/TestMississippiClientBuilder.cs
	 - tests/Reservoir.Blazor.L0Tests/TestMississippiClientBuilder.cs
