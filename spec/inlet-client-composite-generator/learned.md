# Learned Facts

## Current Registration Pattern in Spring.Client

From `samples/Spring/Spring.Client/Program.cs`:

```csharp
// Register features (one line per feature - scales cleanly)
// Write side: aggregate commands
builder.Services.AddBankAccountAggregateFeature();   // ← Source generated

// Navigation/UI: entity selection
builder.Services.AddEntitySelectionFeature();        // ← Hand-written (application-specific)

// Built-in Reservoir features: navigation, lifecycle
builder.Services.AddReservoirBlazorBuiltIns();       // ← Framework code

// Configure Inlet with SignalR effect for real-time projection updates
builder.Services.AddInletClient();
builder.Services.AddInletBlazorSignalR(signalR => signalR
    .WithHubPath("/hubs/inlet")
    .ScanProjectionDtos(typeof(BankAccountBalanceProjectionDto).Assembly));
```

## Source Generator Landscape

### Inlet.Client.Generators (8 generators)

| Generator | Output |
|-----------|--------|
| `CommandClientActionsGenerator` | `{Command}Action`, `{Command}ExecutingAction`, etc. |
| `CommandClientActionEffectsGenerator` | `{Command}ActionEffect` |
| `CommandClientDtoGenerator` | `{Command}RequestDto` |
| `CommandClientMappersGenerator` | `{Command}ActionMapper` |
| `CommandClientReducersGenerator` | `{Aggregate}AggregateReducers` static class |
| `CommandClientStateGenerator` | `{Aggregate}AggregateState` |
| `CommandClientRegistrationGenerator` | `{Aggregate}AggregateFeatureRegistration.Add{Aggregate}AggregateFeature()` |
| `ProjectionClientDtoGenerator` | Projection DTOs with `[ProjectionPath]` |

### Generated Registration Example

`CommandClientRegistrationGenerator` emits per-aggregate feature registration:

```csharp
internal static class BankAccountAggregateFeatureRegistration
{
    public static IServiceCollection AddBankAccountAggregateFeature(this IServiceCollection services)
    {
        // Mappers, Reducers, Action Effects for each command
        return services;
    }
}
```

## Key Observations

1. **Per-aggregate registrations are already generated** - `AddBankAccountAggregateFeature()` is source-generated
2. **Missing: composite/assembly-level generator** - No generator combines all aggregates + Inlet + SignalR
3. **Hub path is conventional** - `/hubs/inlet` is the standard path
4. **Assembly scanning for projections** - `ScanProjectionDtos` discovers `[ProjectionPath]` types

## What Could Be Generated

A composite generator scanning the domain assembly could emit:

```csharp
// In Spring.Client namespace (derived from project)
internal static class SpringInletRegistrations
{
    public static IServiceCollection AddSpringInlet(this IServiceCollection services)
    {
        // 1. Aggregate features (discovered from domain)
        services.AddBankAccountAggregateFeature();
        // ... more aggregates if present

        // 2. Built-in Reservoir features
        services.AddReservoirBlazorBuiltIns();

        // 3. Inlet client + SignalR
        services.AddInletClient();
        services.AddInletBlazorSignalR(signalR => signalR
            .WithHubPath("/hubs/inlet")
            .ScanProjectionDtos(typeof(Spring.Client).Assembly));

        return services;
    }
}
```

## Framework Files

- `src/Inlet.Client.Generators/CommandClientRegistrationGenerator.cs` - Generates per-aggregate feature registration
- `src/Inlet.Client/InletBlazorSignalRBuilder.cs` - Builder for SignalR configuration
- `src/Reservoir.Blazor/BuiltIn/ReservoirBlazorBuiltInRegistrations.cs` - Built-in features

## Open Questions

1. Where does the app name ("Spring") come from?
   - Could derive from assembly name or require an attribute like `[GenerateInletComposite]`
2. Should this include `AddEntitySelectionFeature()` (hand-written, app-specific)?
   - No - that's application-specific state not derived from domain
3. Should hub path be configurable?
   - Default to `/hubs/inlet`; attribute could override
