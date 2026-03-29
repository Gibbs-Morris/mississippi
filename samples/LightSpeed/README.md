# LightSpeed Sample

LightSpeed is the first truthful **base-only Refraction** sample in the repository. It demonstrates a branded operations workbench route built with host-owned theming, seeded in-memory state, and presentational state-down or events-up interaction patterns.

## Purpose

This sample intentionally stays outside the full Mississippi runtime stack. It focuses on proving the Refraction UX layer without introducing Reservoir, event sourcing, or Orleans infrastructure.

The current hero route demonstrates:

- host-owned `RefractionRoot` usage with live brand switching
- a searchable and filterable review queue
- parent-owned selection and detail rendering
- inline review, validation, save, and action flows
- deterministic seeded data that keeps the sample easy to run and reason about

Unlike the comprehensive Spring sample, LightSpeed does not try to prove end-to-end distributed behavior. It proves the first-success Blazor UX path for Refraction itself.

## Running LightSpeed

From the repository root:

```powershell
dotnet run --project samples/LightSpeed/LightSpeed.AppHost/LightSpeed.AppHost.csproj
```

This launches the Aspire AppHost, which orchestrates the Blazor WebAssembly client and its gateway host.

When the sample loads, use the home route to:

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
| Reservoir-backed client state | ✅ Included where needed | ❌ Intentionally excluded in the base-only path |
| Refraction theming and UX composition | ❌ Not the primary proof point | ✅ Primary proof point |
| Verified hero-route tests | ❌ Not the primary proof point | ✅ Focused bUnit coverage in `Refraction.Client.L0Tests` |

LightSpeed is ideal when you want to explore Refraction framework capabilities without the overhead of the full Mississippi stack.
