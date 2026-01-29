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
