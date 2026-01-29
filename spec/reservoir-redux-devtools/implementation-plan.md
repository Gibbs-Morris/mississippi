# Implementation Plan (Revised)

## Step-by-step checklist

1. Update Reservoir Store surface for tool integration.
	- Add protected snapshot accessors in Store (e.g., GetFeatureStateSnapshot).
	- Add protected state replacement method that updates feature states and notifies listeners without running effects.
	- Ensure no changes to IStore interface if possible.

2. Add Reservoir.Blazor DevTools integration (opt-in).
	- Add ReservoirDevToolsOptions (options pattern) to configure name, latency, maxAge, features, sanitizers, and enablement.
	- Add ReservoirDevToolsStore : Store that overrides OnActionDispatched to send action/state to DevTools.
	- Add JS interop bridge (IJSRuntime) to connect/init/send and receive messages.
	- Add handling for DevTools DISPATCH messages: JUMP_TO_STATE, JUMP_TO_ACTION, COMMIT, RESET, ROLLBACK, IMPORT_STATE (best-effort).
	- Ensure DevTools messages do not trigger effects or action dispatch loops.

3. Add DI registration entry point in Reservoir.Blazor.
	- Add AddReservoirDevTools extension method that registers DevTools services and replaces IStore registration for the scope.
	- Make it safe to call after AddReservoir and compatible with existing patterns.

4. Add JS static asset for DevTools bridge.
	- Add wwwroot/mississippi.reservoir.devtools.js to Reservoir.Blazor.
	- Implement a global object to manage connect/init/send/subscribe and handle missing extension gracefully.

5. Tests (L0).
	- Reservoir.L0Tests: add tests for snapshot retrieval and state replacement behavior.
	- Reservoir.Blazor.L0Tests: add tests for DevTools store hook invocations with a fake bridge.

6. Docs (if required).
	- Add a short doc note (or README update) describing enabling DevTools in development and ensuring it is disabled in production.

## File/module touch list (expected)

- src/Reservoir/Store.cs (protected snapshot/replace hooks)
- src/Reservoir/ReservoirRegistrations.cs (if needed for new store type)
- src/Reservoir.Blazor/ReservoirDevToolsOptions.cs (new)
- src/Reservoir.Blazor/ReservoirDevToolsStore.cs (new)
- src/Reservoir.Blazor/ReservoirDevToolsRegistrations.cs (new)
- src/Reservoir.Blazor/Interop/ReservoirDevToolsInterop.cs (new)
- src/Reservoir.Blazor/wwwroot/mississippi.reservoir.devtools.js (new)
- tests/Reservoir.L0Tests/* (new tests)
- tests/Reservoir.Blazor.L0Tests/* (new tests)

## API/compatibility strategy

- Prefer protected Store hooks over IStore interface changes.
- Opt-in integration via DI extension; default behavior unchanged.

## Observability

- Minimal logging in DevTools bridge for connection failures (LoggerExtensions if added).

## Test plan

- Run targeted tests:
  - pwsh ./eng/src/agent-scripts/unit-test-mississippi-solution.ps1 -TestLevels @('L0Tests')
- If build errors arise, run cleanup:
  - pwsh ./clean-up.ps1

## Rollout and backout

- Rollout: enable by calling AddReservoirDevTools in development.
- Backout: remove AddReservoirDevTools registration; no data migrations required.

## Risks and mitigations

- Large state payloads: allow latency/maxAge configuration and sanitizers.
- State deserialization failures: log and ignore failed time-travel requests.

## Constraints

- Avoid new dependencies unless explicitly approved.
- Keep changes additive with no breaking API changes unless required.
