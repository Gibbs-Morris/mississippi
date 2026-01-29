# Verification

## Claim list

1. Reservoir store has no existing DevTools integration and no JS interop hooks.
2. Reservoir.Blazor is the correct place for JS interop integration.
3. Redux DevTools can be integrated via window.__REDUX_DEVTOOLS_EXTENSION__.connect and send/init.
4. DevTools messages can request state changes (jump/import) that Reservoir must handle.
5. Integration can be disabled in production via DI registration and configuration.
6. Adding integration will not require a new project if Reservoir.Blazor can host JS interop assets.

## Verification questions (initial)

1. Does Reservoir already expose any event hooks beyond Subscribe that can be used for DevTools?
2. Are there existing JS interop patterns or JS asset pipelines in Reservoir.Blazor or other Blazor projects in the repo?
3. Is Reservoir store scoped per DI (scoped lifetime) and how does this affect per-instance DevTools connections?
4. What is the expected action type format in Reservoir actions (do they have a Type or name string)?
5. Does the Store or RootReducer expose a way to replace state (needed for time-travel)?
6. Are there existing middleware implementations that log actions or state transitions?
7. Where should DI extension methods live for enabling optional features in Reservoir.Blazor?
8. Does the project allow adding a JS file under wwwroot in Reservoir.Blazor, or is there a different asset bundling approach?
9. Are there any tests for Reservoir.Blazor or Store that would need updating for new middleware hooks?
10. What is the current policy on adding third-party dependencies (C# or JS) in this repository?

## Coverage mapping

- Claims 1-2: Questions 1-3.
- Claims 3-4: Questions 4-6.
- Claim 5: Questions 7 and 10.
- Claim 6: Questions 8-9.

## Answers (evidence-based)

1. The Store exposes a protected virtual OnActionDispatched hook and a public Subscribe method but no public event or hook beyond that. (src/Reservoir/Store.cs)
2. No JS interop usage was found in Reservoir.Blazor or other src files; there are no IJSRuntime/JSImport references. (grep: IJSRuntime/JSImport across src)
3. IStore is registered as scoped in AddReservoir; each DI scope gets its own Store instance. (src/Reservoir/ReservoirRegistrations.cs)
4. IAction is a marker interface with no required type or name; action identity must be derived from runtime type. (src/Reservoir.Abstractions/Actions/IAction.cs)
5. The Store exposes no public method to replace state; featureStates is private and only updated through reducers. (src/Reservoir/Store.cs, src/Reservoir.Abstractions/IStore.cs)
6. There are no built-in middleware implementations in production code; middleware appears only in tests. (src/Reservoir.Abstractions/IMiddleware.cs, tests/Reservoir.L0Tests/StoreTests.cs)
7. Reservoir.Blazor uses static extension methods for opt-in features under BuiltIn registrations; similar pattern can be followed for DevTools. (src/Reservoir.Blazor/BuiltIn/ReservoirBlazorBuiltInRegistrations.cs)
8. Reservoir.Blazor does not currently include a wwwroot folder; adding static assets would be new but supported by Razor class library conventions. (src/Reservoir.Blazor)
9. Reservoir and Reservoir.Blazor already have L0 test projects; new middleware/state-replacement hooks would require new L0 tests. (tests/Reservoir.L0Tests, tests/Reservoir.Blazor.L0Tests)
10. Repository rules prohibit adding new third-party dependencies without explicit approval. (src/.. instructions: .github/instructions/csharp.instructions.md)
