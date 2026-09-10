# LightSpeed Sample

LightSpeed is a minimal Blazor WebAssembly sample application designed to demonstrate the **Refraction** framework and showcase control usage patterns.

## Purpose

This sample provides a stripped-down implementation with no domain logic, event sourcing, or Orleans grains. It focuses exclusively on demonstrating:

- Refraction framework integration
- Blazor WebAssembly control patterns
- Reservoir actions, pure reducers, selectors, and presentational callbacks
- Scoped dark, light, and high-contrast themes
- A home page and interactive kitchen sink at `/kitchen-sink`

Unlike the comprehensive Spring sample, LightSpeed is intentionally kept minimal to serve as a clean starting point for experimenting with Refraction controls without the complexity of a full event-sourced architecture.

## Running LightSpeed

From the repository root:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --no-launch-profile --project samples/LightSpeed/LightSpeed.Gateway/LightSpeed.Gateway.csproj --urls http://127.0.0.1:5278
```

Open `http://127.0.0.1:5278`. The gateway hosts the Blazor WebAssembly client.
This local HTTP route needs no external services or certificate trust step.
The included Aspire AppHost is an optional orchestration entry point for
environments with Aspire's local prerequisites configured.

## Structure

- **LightSpeed.Client** - Blazor WebAssembly application with Refraction controls
- **LightSpeed.Gateway** - ASP.NET Core host for the Blazor WebAssembly app
- **LightSpeed.AppHost** - Aspire orchestration for local development
- **LightSpeed.Client.L0Tests** - Reducer, selector, and store-registration examples

## Explore the state flow

Open the kitchen sink, edit the work email, and select **Validate profile**.
The form emits callbacks; its page dispatches actions to Reservoir.
Pure reducers update the feature state, and selectors produce the values
rendered by the form and state inspector. Reset restores the example address
while preserving the chosen theme. State lasts only for the current browser
session and is not persisted to a server.

The gallery identifies the verified input/theme surface separately from the
library's prototype controls. It is not a whole-library accessibility
certification. Components are organized into atomic folders, with page-level
store integration and separate markup, logic, and styles.

## Comparison with Spring

| Feature | Spring | LightSpeed |
|---------|--------|------------|
| Domain model | ✅ Full event-sourced aggregates | ❌ None |
| Orleans grains | ✅ Runtime host (Orleans silo) with distributed actors | ❌ None |
| Event sourcing | ✅ Commands, events, projections | ❌ None |
| Real-time updates | ✅ SignalR with Inlet | ❌ None |
| Refraction controls | ❌ Not focused | ✅ Primary focus |
| Tests | ✅ L0 and L2 tests | ✅ Local state and registration tests |

LightSpeed is ideal when you want to explore Refraction framework capabilities without the overhead of the full Mississippi stack.
