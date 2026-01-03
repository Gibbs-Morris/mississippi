# Ripples Attribute Design (Revised v2)

**Status**: 🔵 Design Document  
**Supersedes**: Previous attribute placement designs

## Design Philosophy

**Aggregates** and **Projections** are fundamentally different:

| Aspect | Aggregate | Projection |
|--------|-----------|------------|
| **What is it?** | An actor that receives commands | A data product for the UI |
| **Grain role** | IS the domain concept | Mechanism to build data |
| **State role** | Internal to the grain | THE thing being exposed |
| **Identity** | The grain interface | The projection record |

Therefore:
- **Aggregates**: `[UxAggregate]` goes on the **grain interface** (the actor)
- **Projections**: `[UxProjection]` goes on the **projection record** (the data)

## Revised Design: Data-Centric Projection Attributes

All projection configuration lives on the **projection record**:

```csharp
// ═══════════════════════════════════════════════════════════════
// PROJECTION - All configuration on the data record
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Channel messages projection - all metadata in one place.
/// </summary>
[UxProjection(
    Route = "channel-messages",           // API route
    BrookName = "cascade.chat.messages")] // Event stream to subscribe to
[SnapshotStorageName("CASCADE", "CHAT", "CHANNELMESSAGES")]
[GenerateSerializer]
[Alias("Cascade.Domain.Projections.ChannelMessages.ChannelMessagesProjection")]
public sealed record ChannelMessagesProjection
{
    [Id(0)] public string ChannelId { get; init; } = string.Empty;
    [Id(1)] public ImmutableList<MessageItem> Messages { get; init; } = ImmutableList<MessageItem>.Empty;
    [Id(2)] public int MessageCount { get; init; }
}

// ═══════════════════════════════════════════════════════════════
// GRAIN - Just behavior, no configuration
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Grain reads configuration from the projection type's attributes.
/// </summary>
public sealed class ChannelMessagesProjectionGrain 
    : UxProjectionGrainBase<ChannelMessagesProjection>
{
    // Base class automatically:
    // - Reads [UxProjection].BrookName to subscribe to brook
    // - Reads [SnapshotStorageName] for persistence
    // - No explicit configuration needed here
}


// ═══════════════════════════════════════════════════════════════
// AGGREGATE - Unchanged (attribute on grain interface)
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Aggregate grain interface - the actor definition.
/// </summary>
[UxAggregate(Route = "channels")]
public interface IChannelAggregateGrain : IAggregateGrain<ChannelState>
{
    [CommandRoute("create")]
    Task<OperationResult> CreateAsync(CreateChannelCommand command);
}

/// <summary>
/// Aggregate state - internal to the grain.
/// </summary>
[GenerateSerializer]
internal sealed record ChannelState { ... }
```

## Why This Is Better

### 1. Single Source of Truth

All projection metadata in one place:

```csharp
[UxProjection(Route = "...", BrookName = "...")]  // API + Stream
[SnapshotStorageName("...", "...", "...")]        // Storage
[GenerateSerializer]                               // Serialization
[Alias("...")]                                     // Versioning
public sealed record MyProjection { ... }
```

### 2. Less Boilerplate

No custom grain interface needed:

```csharp
// ❌ Before: Required custom interface
[UxProjection]
public interface IChannelMessagesProjectionGrain 
    : IUxProjectionGrain<ChannelMessagesProjection> { }

// ✅ After: No interface needed, just the record
[UxProjection(Route = "channel-messages", BrookName = "...")]
public sealed record ChannelMessagesProjection { ... }
```

### 3. Matches Existing Patterns

Already using record-level attributes:
- `[SnapshotStorageName]` - on record ✓
- `[GenerateSerializer]` - on record ✓
- `[Alias]` - on record ✓
- `[UxProjection]` - on record ✓ (now consistent)

### 4. Source Generator Friendly

Generators find everything by scanning projection records:

```csharp
// Generator finds all [UxProjection] types
foreach (var projection in context.GetTypesWithAttribute<UxProjectionAttribute>())
{
    var route = projection.GetAttribute<UxProjectionAttribute>().Route;
    var brookName = projection.GetAttribute<UxProjectionAttribute>().BrookName;
    var storageName = projection.GetAttribute<SnapshotStorageNameAttribute>();
    
    // Generate controller, route registry, ripple registration...
}
```

## Pattern Comparison

```
┌─────────────────────────────────────────────────────────────────┐
│                      FINAL PATTERN                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  AGGREGATES                      PROJECTIONS                     │
│  ──────────                      ───────────                     │
│                                                                  │
│  [UxAggregate]                   [UxProjection]                  │
│  interface IMyAggregateGrain     record MyProjection             │
│      : IAggregateGrain<TState>   {                               │
│  {                                   // fields                   │
│      // command methods          }                               │
│  }                                                               │
│                                                                  │
│  record MyState { }              class MyProjectionGrain         │
│  (internal to grain)                 : UxProjectionGrainBase<T>  │
│                                  { /* no config, just behavior */}│
│                                                                  │
│  WHY: Grain IS the actor         WHY: Record IS the product      │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## The `[UxProjection]` Attribute

```csharp
/// <summary>
/// Marks a projection record for API exposure and stream subscription.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class UxProjectionAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the API route segment. Defaults to kebab-case of type name.
    /// </summary>
    /// <example>"channel-messages" → /api/projections/channel-messages/{entityId}</example>
    public string? Route { get; set; }
    
    /// <summary>
    /// Gets or sets the brook (event stream) name to subscribe to.
    /// </summary>
    /// <example>"cascade.chat.messages"</example>
    public string? BrookName { get; set; }
    
    /// <summary>
    /// Gets or sets whether to generate a batch endpoint. Default true.
    /// </summary>
    public bool EnableBatch { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the authorization policy name.
    /// </summary>
    public string? Authorize { get; set; }
    
    /// <summary>
    /// Gets or sets OpenAPI tags.
    /// </summary>
    public string[]? Tags { get; set; }
}
```

## How the Grain Reads Configuration

The base class uses reflection (cached) or source-gen to read attributes:

```csharp
public abstract class UxProjectionGrainBase<TProjection> : Grain, IGrainBase, IUxProjectionGrain<TProjection>
    where TProjection : class
{
    private static readonly Lazy<UxProjectionMetadata> Metadata = new(() => 
        UxProjectionMetadataReader.Read<TProjection>());
    
    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await base.OnActivateAsync(ct);
        
        // Subscribe to brook from attribute
        if (Metadata.Value.BrookName is { } brookName)
        {
            await SubscribeToBrookAsync(brookName);
        }
    }
    
    // Snapshot storage name read from [SnapshotStorageName] on TProjection
    protected override string GetSnapshotStorageName() 
        => Metadata.Value.SnapshotStorageName;
}

internal static class UxProjectionMetadataReader
{
    public static UxProjectionMetadata Read<T>()
    {
        var type = typeof(T);
        var uxAttr = type.GetCustomAttribute<UxProjectionAttribute>();
        var storageAttr = type.GetCustomAttribute<SnapshotStorageNameAttribute>();
        
        return new UxProjectionMetadata(
            Route: uxAttr?.Route ?? ToKebabCase(type.Name),
            BrookName: uxAttr?.BrookName,
            SnapshotStorageName: storageAttr?.GetFullName() ?? type.Name);
    }
}
```

## Migration Example

### Before

```csharp
// Projection record with some attributes
[SnapshotStorageName("CASCADE", "CHAT", "CHANNELMESSAGES")]
[GenerateSerializer]
[Alias("...")]
internal sealed record ChannelMessagesProjection { ... }

// Grain with brook name configuration
public class ChannelMessagesProjectionGrain 
    : UxProjectionGrainBase<ChannelMessagesProjection>
{
    public ChannelMessagesProjectionGrain() 
        : base("cascade.chat.messages")  // Brook name in constructor
    {
    }
}
```

### After

```csharp
// Everything on the projection record
[UxProjection(Route = "channel-messages", BrookName = "cascade.chat.messages")]
[SnapshotStorageName("CASCADE", "CHAT", "CHANNELMESSAGES")]
[GenerateSerializer]
[Alias("...")]
public sealed record ChannelMessagesProjection { ... }  // Now public for API

// Grain is just behavior - no configuration
public sealed class ChannelMessagesProjectionGrain 
    : UxProjectionGrainBase<ChannelMessagesProjection>
{
    // Empty! Base class reads everything from TProjection attributes
}
```

## What Gets Generated

From `[UxProjection]` on a record, generators create:

1. **Controller** with GET/batch endpoints
2. **Route registry** entry for WASM
3. **Ripple registration** for DI
4. **Grain interface** (if needed for custom methods)

```csharp
// All auto-generated from [UxProjection] on ChannelMessagesProjection

[Route("api/projections/channel-messages/{entityId}")]
public sealed class ChannelMessagesProjectionController : UxProjectionControllerBase<ChannelMessagesProjection> { }

public static partial class RouteRegistry
{
    public static class Projections
    {
        public static string ChannelMessagesProjection(string entityId) => $"api/projections/channel-messages/{entityId}";
    }
}

public static partial class RippleRegistrations
{
    // services.AddScoped<IRipple<ChannelMessagesProjection>, ...>();
}
```

## Summary

| Item | Where | Rationale |
|------|-------|-----------|
| `[UxAggregate]` | Grain interface | Grain IS the actor |
| `[UxProjection]` | Projection record | Record IS the product |
| `[SnapshotStorageName]` | Projection record | Storage config with data |
| `BrookName` | Inside `[UxProjection]` | Stream config with data |
| `[GenerateSerializer]` | Both state types | Orleans requirement |

