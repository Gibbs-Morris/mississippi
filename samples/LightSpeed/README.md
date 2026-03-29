# LightSpeed Sample

LightSpeed is the first truthful Refraction proof harness in the repository. It now demonstrates two aligned operations-workbench routes built from the same presentational surface:

- `/` — the **base-only** Refraction hero route with host-owned state and theming
- `/reservoir-operations-workbench` — the **Reservoir-explicit** parity route backed by deterministic in-memory Reservoir reducers

## Purpose

This sample intentionally stays outside the full Mississippi runtime stack. It focuses on proving the Refraction UX layer without introducing event sourcing, Orleans infrastructure, or domain-heavy sample concerns.

The home route remains the default base-only story. The Reservoir route exists to prove that the same workbench surface can run with explicit Reservoir-backed composition without reshaping the base client.

Across the two routes, the sample demonstrates:

- host-owned `RefractionRoot` usage with live brand switching
- a searchable and filterable review queue
- parent-owned selection and detail rendering
- inline review, validation, save, and action flows
- deterministic seeded data that keeps the sample easy to run and reason about
- Reservoir-explicit state projection and action binding on the parity route only

Unlike the comprehensive Spring sample, LightSpeed does not try to prove end-to-end distributed behavior. It proves the first-success Blazor UX path for Refraction itself and then proves honest optional Reservoir composition with a literal parity route.

## Running LightSpeed

From the repository root:

```powershell
dotnet run --project samples/LightSpeed/LightSpeed.AppHost/LightSpeed.AppHost.csproj
```

This launches the Aspire AppHost, which orchestrates the Blazor WebAssembly client and its gateway host.

When the sample loads, use the home route (`/`) or the Reservoir parity route (`/reservoir-operations-workbench`) to:

1. switch between the `horizon` and `signal` brands,
2. search and filter the live queue,
3. select a work item,
4. review and edit the response plan, and
5. validate and apply the next action.

## Structure

- **LightSpeed.Client** - Blazor WebAssembly application with Refraction controls
- **LightSpeed.Gateway** - ASP.NET Core host for the Blazor WebAssembly app
- **LightSpeed.AppHost** - Aspire orchestration for local development
- **Refraction.Client.L0Tests** - shared bUnit test project that verifies the LightSpeed hero route behavior

## Comparison with Spring

| Feature | Spring | LightSpeed |
|---------|--------|------------|
| Domain model | ✅ Full event-sourced aggregates | ❌ None |
| Orleans grains | ✅ Runtime host (Orleans silo) with distributed actors | ❌ None |
| Event sourcing | ✅ Commands, events, projections | ❌ None |
| Real-time updates | ✅ SignalR with Inlet | ❌ None |
| Reservoir-backed client state | ✅ Included where needed | ✅ Explicit parity route only; home route stays base-only |
| Refraction theming and UX composition | ❌ Not the primary proof point | ✅ Primary proof point |
| Verified hero-route tests | ❌ Not the primary proof point | ✅ Focused bUnit coverage in `Refraction.Client.L0Tests` |

LightSpeed is ideal when you want to explore Refraction framework capabilities without the overhead of the full Mississippi stack while still seeing an honest base-only path and an explicit Reservoir-backed continuation.
