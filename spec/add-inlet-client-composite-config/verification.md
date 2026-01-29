# Verification

## Claim list
1. Default `Add{App}Inlet()` remains and preserves behavior.
2. New overload allows optional SignalR configuration without changing defaults.
3. Configuration is applied after defaults to allow overrides.
4. Generator tests cover the overload and invocation.
5. No docs updates are required; doc plan will be recorded.

## Questions
1. Does the generator emit an overload with `Action<InletBlazorSignalRBuilder>?`?
2. Does `Add{App}Inlet()` forward to the overload with `null`?
3. Is `configureSignalR` invoked after `WithHubPath` and DTO registration?
4. Are both projections-present and projections-absent paths invoking `configureSignalR`?
5. Is there a new L0 test for the generator output?
6. Do L0 generator tests pass?
7. Are there docs references that require updates?

## Answers
1. Yes. The generator emits an overload with `Action<InletBlazorSignalRBuilder>? configureSignalR`. Evidence: [src/Inlet.Client.Generators/InletClientCompositeGenerator.cs](src/Inlet.Client.Generators/InletClientCompositeGenerator.cs#L183-L194).
2. Yes. The default method returns `Add{App}Inlet(services, null)`. Evidence: [src/Inlet.Client.Generators/InletClientCompositeGenerator.cs](src/Inlet.Client.Generators/InletClientCompositeGenerator.cs#L168-L181).
3. Yes. `configureSignalR?.Invoke(signalR);` runs after `WithHubPath` and registrations. Evidence: [src/Inlet.Client.Generators/InletClientCompositeGenerator.cs](src/Inlet.Client.Generators/InletClientCompositeGenerator.cs#L219-L248).
4. Yes. Both branches include `configureSignalR?.Invoke(signalR);`. Evidence: [src/Inlet.Client.Generators/InletClientCompositeGenerator.cs](src/Inlet.Client.Generators/InletClientCompositeGenerator.cs#L219-L248).
5. Yes. New test asserts overload and invocation. Evidence: [tests/Inlet.Client.Generators.L0Tests/InletClientCompositeGeneratorTests.cs](tests/Inlet.Client.Generators.L0Tests/InletClientCompositeGeneratorTests.cs#L105-L137).
6. Yes. `test-project-quality.ps1 -TestProject Inlet.Client.Generators.L0Tests -SkipMutation` passed (75/75).
7. No. Search in `docs/` found no references to composite registration or `Add{App}Inlet`.
