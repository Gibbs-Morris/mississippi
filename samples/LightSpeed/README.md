# LightSpeed Sample

LightSpeed is a minimal Blazor WebAssembly sample application designed to demonstrate the **Refraction** framework and showcase control usage patterns.

## Purpose

This sample provides a stripped-down implementation with no domain logic, event sourcing, or Orleans grains. It focuses exclusively on demonstrating:

- Refraction framework integration
- Blazor WebAssembly control patterns
- Minimal bootstrapping for rapid prototyping

Unlike the comprehensive Spring sample, LightSpeed is intentionally kept minimal to serve as a clean starting point for experimenting with Refraction controls without the complexity of a full event-sourced architecture.

## Running LightSpeed

From the repository root:

```powershell
dotnet run --project samples/LightSpeed/LightSpeed.AppHost/LightSpeed.AppHost.csproj
```

This launches the Aspire AppHost, which orchestrates the Blazor WebAssembly client and its host server.

## Structure

- **LightSpeed.Client** - Blazor WebAssembly application with Refraction controls
- **LightSpeed.Server** - ASP.NET Core host for the Blazor WebAssembly app
- **LightSpeed.AppHost** - Aspire orchestration for local development

## Comparison with Spring

| Feature | Spring | LightSpeed |
|---------|--------|------------|
| Domain model | ✅ Full event-sourced aggregates | ❌ None |
| Orleans grains | ✅ Silo with distributed actors | ❌ None |
| Event sourcing | ✅ Commands, events, projections | ❌ None |
| Real-time updates | ✅ SignalR with Inlet | ❌ None |
| Refraction controls | ❌ Not focused | ✅ Primary focus |
| Tests | ✅ L0 and L2 tests | ❌ None |

LightSpeed is ideal when you want to explore Refraction framework capabilities without the overhead of the full Mississippi stack.

## Refraction Theming Sample

LightSpeed uses the standalone **Luminous** theme package assets directly.

The client host loads styles in this order:

1. Luminous tokens (`_content/Mississippi.Refraction.Theme.Luminous/refraction.tokens.luminous.css`)
2. Luminous base document styles (`_content/Mississippi.Refraction.Theme.Luminous/refraction.luminous.base.css`)
3. Luminous component styles (`_content/Mississippi.Refraction.Theme.Luminous/refraction.theme.luminous.css`)
4. App scoped styles (`LightSpeed.Client.styles.css`)

This keeps the theme opinionated and self-contained while still allowing app-level overrides.

### Create your own theme package

For a custom company theme, publish a NuGet package that ships a CSS file defining the same `--mis-*` tokens, for example:

- `MyCompany.Refraction.Theme.MaterialDesign`

Consumer apps then load:

1. Your package token CSS
2. Your package base CSS
3. Your package component CSS
4. Optional app-specific overrides

Only add tokens when a new component need appears. Keep the shared token contract small and stable.
