---
applyTo: '**/*.cs'
---

# Storage Type Naming with Attributes

Governing thought: Persisted types (events, snapshots, commands) use stable string identifiers via attributes so code can be freely refactored without breaking stored data or requiring migrations.

## Rules (RFC 2119)

- Persisted types **MUST** have a naming attribute (e.g., `[EventName]`, `[SnapshotName]`) applied.  
  Why: The attribute provides a stable, version-aware identifier that survives refactoring.
- Naming attributes **MUST** have a `version` parameter (int) that is combined with appName/moduleName/name to form the full identifier.
  Why: Separating version as a typed field prevents typos and enables programmatic version queries.
- The computed `EventName` property **MUST** follow the `APPNAME.MODULENAME.NAME.Vn` pattern (e.g., `ORDER.FULFILLMENT.SHIPPED.V1`).
  Why: Dot-separated segments enable hierarchical organization and filtering; version suffix enables schema evolution.
- AppName, ModuleName, and Name values **MUST NOT** change once types have been persisted to production storage.
  Why: Changing these values breaks deserialization of existing stored data.
- When evolving a type's schema, developers **MUST** increment the `version` parameter rather than modifying appName/moduleName/name.
  Why: Maintains backward compatibility with previously stored data.
- A type registry (e.g., `IEventTypeRegistry`, `ISnapshotTypeRegistry`) **MUST** be used for bidirectional type ↔ name resolution.
  Why: Centralizes lookup logic and enables O(1) cached resolution without per-call reflection.
- Developers **SHOULD** register all persisted types at startup via assembly scanning.  
  Why: Validates all types are properly attributed and builds the lookup cache once.
- Class/record names **MAY** be freely refactored without affecting stored data.  
  Why: Only the attribute value is persisted, not the CLR type name.

## Scope and Audience

**Audience:** Developers creating or consuming persisted types (events, snapshots, commands) in the Mississippi framework.

**In scope:** Naming conventions, attribute usage, type registry, refactoring safety.

**Out of scope:** Serialization format details, storage provider implementation.

## At-a-Glance Quick-Start

1. Decorate your event/snapshot with the appropriate naming attribute.
2. Use a stable, versioned name that won't change.
3. Refactor class names freely—only the attribute matters for storage.
4. When schema changes, create a new version with a new attribute.

```csharp
// ✅ Correct: Stable attribute with version as int, class name can change
[EventName("ORDER", "FULFILLMENT", "SHIPPED", version: 1)]
public sealed record OrderShippedEvent(Guid OrderId, DateTimeOffset ShippedAt);

// Later, refactor the class name without breaking storage:
[EventName("ORDER", "FULFILLMENT", "SHIPPED", version: 1)]  // Same attribute!
public sealed record OrderShipmentCompletedEvent(Guid OrderId, DateTimeOffset ShippedAt);
```

## Purpose

This pattern solves a fundamental problem in event-sourced and snapshot-based systems: **how do you refactor code without breaking persisted data?**

Traditional approaches store the fully-qualified CLR type name (e.g., `MyApp.Orders.OrderShippedEvent`). This creates tight coupling between your code structure and your database, making refactoring risky and migrations expensive.

The attribute-based naming pattern decouples the **storage identity** from the **code identity**, giving developers freedom to:

- Rename classes, records, and namespaces
- Move types between projects or assemblies
- Reorganize code structure
- All without touching stored data or writing migrations

## Core Principles

### Separation of Concerns

| Concern              | Responsibility       | Example                            |
| -------------------- | -------------------- | ---------------------------------- |
| **Storage Identity** | `EventName` property | `"ORDER.FULFILLMENT.SHIPPED.V1"`   |
| **Code Identity**    | Class/record name    | `OrderShippedEvent`                |
| **Schema Version**   | Version suffix       | `V1`, `V2`, etc.                   |

The storage layer only knows about attribute values. The code layer only knows about CLR types. The registry bridges them.

### Type Registries

Each category of persisted type has its own registry. For example, `IEventTypeRegistry` handles events, while snapshots would use `ISnapshotTypeRegistry`. The interface pattern is the same:

```csharp
// Example: Event registry (similar pattern for snapshots, commands, etc.)
public interface IEventTypeRegistry
{
    // Storage → Code: Find the CLR type for a stored event name
    Type? ResolveType(string eventName);
    
    // Code → Storage: Find the storage name for a CLR type
    string? ResolveName(Type eventType);
    
    // Get all registered types (for validation/diagnostics)
    IReadOnlyDictionary<string, Type> GetRegisteredTypes();
}

// Snapshots would follow the same pattern
public interface ISnapshotTypeRegistry
{
    Type? ResolveType(string snapshotName);
    string? ResolveName(Type snapshotType);
    IReadOnlyDictionary<string, Type> GetRegisteredTypes();
}
```

At startup, assembly scanning populates both lookup dictionaries. Runtime lookups are O(1) dictionary access—no reflection.

> **Note:** Each registry is independent. Events use `[EventName]` with `IEventTypeRegistry`, snapshots use `[SnapshotName]` with `ISnapshotTypeRegistry`, and so on. This separation keeps concerns clean and allows different scanning/validation rules per type category.

### Deserialization

Once the registry resolves a storage name to a CLR type, deserialization is handled by `ISerializationReader.Deserialize(Type, ReadOnlyMemory<byte>)`:

```csharp
// Resolve storage name to CLR type
Type? eventType = registry.ResolveType(storedEventName);

// Deserialize using the resolved type
object @event = serializationProvider.Deserialize(eventType!, eventData);
```

This approach:

- Uses the serializer's native type-based deserialization (e.g., `JsonSerializer.Deserialize(data, type)`)
- Avoids the complexity of expression tree compilation or delegate caching
- Maintains type safety through the registry lookup

## Naming Convention

### Pattern: `APPNAME.MODULENAME.NAME.Vn`

```text
ORDER.FULFILLMENT.SHIPPED.V1
│     │           │       │
│     │           │       └── Version (schema evolution)
│     │           └────────── Event/Action name (UPPERCASE)
│     └────────────────────── Module name (UPPERCASE)
└──────────────────────────── Application name (UPPERCASE)
```

### Examples

| Type                | Attribute Name                       |
| ------------------- | ------------------------------------ |
| Order shipped       | `ORDER.FULFILLMENT.SHIPPED.V1`       |
| Order cancelled     | `ORDER.LIFECYCLE.CANCELLED.V1`       |
| Customer registered | `CUSTOMER.ONBOARDING.REGISTERED.V1`  |
| Inventory adjusted  | `INVENTORY.STOCK.ADJUSTED.V1`        |
| Payment snapshot    | `PAYMENT.ACCOUNT.SNAPSHOT.V1`        |

### Why This Format?

1. **Uppercase**: Consistent, unambiguous, grep-friendly
2. **Dot-separated**: Hierarchical, filterable, familiar (like namespaces)
3. **Version suffix**: Explicit schema evolution tracking
4. **Human-readable**: Easy to understand in logs, storage browsers, debugging

## Schema Evolution

When you need to change an event's structure:

### Adding Optional Fields (Non-Breaking)

You can add nullable/optional fields without incrementing the version:

```csharp
// Original
[EventName("ORDER", "FULFILLMENT", "SHIPPED", version: 1)]
public sealed record OrderShippedEvent(Guid OrderId, DateTimeOffset ShippedAt);

// Extended (still version: 1 - backward compatible)
[EventName("ORDER", "FULFILLMENT", "SHIPPED", version: 1)]
public sealed record OrderShippedEvent(
    Guid OrderId, 
    DateTimeOffset ShippedAt,
    string? TrackingNumber = null);  // Optional, defaults to null
```

### Breaking Changes (Increment Version)

For breaking changes, increment the `version` parameter:

```csharp
// Version 1 - Keep for reading old events
[EventName("ORDER", "FULFILLMENT", "SHIPPED", version: 1)]
public sealed record OrderShippedEventV1(Guid OrderId, DateTimeOffset ShippedAt);

// Version 2 - New structure for new events
[EventName("ORDER", "FULFILLMENT", "SHIPPED", version: 2)]
public sealed record OrderShippedEvent(
    Guid OrderId, 
    DateTimeOffset ShippedAt,
    ShippingCarrier Carrier,      // Required field - breaking change
    string TrackingNumber);        // Required field - breaking change
```

Your reducer/projector handles both versions:

```csharp
public OrderState Reduce(OrderState state, object @event) => @event switch
{
    OrderShippedEventV1 e => state with { Status = "Shipped", ShippedAt = e.ShippedAt },
    OrderShippedEvent e => state with { 
        Status = "Shipped", 
        ShippedAt = e.ShippedAt,
        Carrier = e.Carrier,
        TrackingNumber = e.TrackingNumber 
    },
    _ => state
};
```

## Refactoring Scenarios

### Scenario 1: Rename a Class

**Before:**

```csharp
[EventName("ORDER", "FULFILLMENT", "SHIPPED", version: 1)]
public sealed record OrderShippedEvent(Guid OrderId, DateTimeOffset ShippedAt);
```

**After:**

```csharp
[EventName("ORDER", "FULFILLMENT", "SHIPPED", version: 1)]  // Unchanged!
public sealed record OrderShipmentCompletedEvent(Guid OrderId, DateTimeOffset ShippedAt);
```

✅ No migration needed. Stored events still deserialize correctly.

### Scenario 2: Move to Different Namespace

**Before:**

```csharp
namespace MyApp.Orders.Events;

[EventName("ORDER", "FULFILLMENT", "SHIPPED", version: 1)]
public sealed record OrderShippedEvent(...);
```

**After:**

```csharp
namespace MyApp.Domain.Orders.Fulfillment.Events;  // New namespace

[EventName("ORDER", "FULFILLMENT", "SHIPPED", version: 1)]  // Unchanged!
public sealed record OrderShippedEvent(...);
```

✅ No migration needed.

### Scenario 3: Move to Different Assembly

**Before:** `MyApp.Orders.dll`  
**After:** `MyApp.Domain.dll`

✅ No migration needed—just ensure the new assembly is scanned at startup.

### Scenario 4: Split a Monolith

When extracting a microservice, the events go with it:

```csharp
// Same event, now in a different service
[EventName("ORDER", "FULFILLMENT", "SHIPPED", version: 1)]
public sealed record OrderShippedEvent(...);
```

✅ Cross-service event sharing works because the attribute name is the contract.

## Implementation Details

### Attribute Definition

Each persisted type category has its own attribute. The `version` is a constructor parameter (with default value 1), and the attribute computes the full storage name:

```csharp
/// <summary>
/// Defines the storage name for an event type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EventNameAttribute : Attribute
{
    public string AppName { get; }
    public string ModuleName { get; }
    public string Name { get; }
    public int Version { get; }
    
    /// <summary>
    /// Gets the full storage name in the format APPNAME.MODULENAME.NAME.Vn.
    /// </summary>
    public string EventName => $"{AppName}.{ModuleName}.{Name}.V{Version}";

    public EventNameAttribute(string appName, string moduleName, string name, int version = 1)
    {
        AppName = appName;
        ModuleName = moduleName;
        Name = name;
        Version = version;
    }
}

/// <summary>
/// Defines the storage name for a snapshot type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SnapshotNameAttribute : Attribute
{
    public string AppName { get; }
    public string ModuleName { get; }
    public string Name { get; }
    public int Version { get; }
    
    public string SnapshotName => $"{AppName}.{ModuleName}.{Name}.V{Version}";

    public SnapshotNameAttribute(string appName, string moduleName, string name, int version = 1)
    {
        AppName = appName;
        ModuleName = moduleName;
        Name = name;
        Version = version;
    }
}
```

> **Key insight:** The `Version` is a separate `int` field, not embedded in a string. This:
>
> - Prevents typos like `"SHIPPED.V1"` vs `"SHIPPEDV1"` vs `"SHIPPED1"`
> - Enables programmatic queries like "find all V1 events"
> - Makes version bumps explicit and IDE-refactorable

### Startup Registration

```csharp
// In your DI setup
services.AddEventTypeRegistry(options =>
{
    // Scan assemblies for attributed types
    options.ScanAssemblies(
        typeof(OrderShippedEvent).Assembly,
        typeof(CustomerRegisteredEvent).Assembly);
});
```

### Runtime Flow

**Writing Events:**

```text
Event Object → Registry.ResolveName(type) → Attribute Name → Serialize → Storage
     ↓                    ↓                        ↓
OrderShippedEvent    "ORDER.FULFILLMENT.SHIPPED.V1"    { eventType: "...", data: ... }
```

**Reading Events:**

```text
Storage → Attribute Name → Registry.ResolveType(name) → DeserializerFactory.GetDeserializer(type) → Event Object
   ↓            ↓                     ↓                              ↓                                    ↓
{ ... }  "ORDER.FULFILLMENT..."  typeof(OrderShippedEvent)   Func<bytes, object>              OrderShippedEvent
```

## Validation and Diagnostics

### Startup Validation

The registry validates at startup:

- All scanned types have the required attribute
- No duplicate attribute names across types
- All attribute names follow the expected pattern

```csharp
// Throws if validation fails
services.AddEventTypeRegistry(options =>
{
    options.ScanAssemblies(...);
    options.ValidateOnStartup = true;  // Default: true
});
```

### Diagnostic Queries

```csharp
// List all registered events
var registry = serviceProvider.GetRequiredService<IEventTypeRegistry>();
foreach (var (name, type) in registry.GetRegisteredTypes())
{
    Console.WriteLine($"{name} -> {type.Name}");
}
```

## Applicability Beyond Events

This pattern applies to any persisted type. Each category has its own attribute and registry:

| Domain    | Attribute         | Registry                 | Example                                                          |
| --------- | ----------------- | ------------------------ | ---------------------------------------------------------------- |
| Events    | `[EventName]`     | `IEventTypeRegistry`     | `[EventName("ORDER", "LIFECYCLE", "CREATED", version: 1)]`       |
| Snapshots | `[SnapshotName]`  | `ISnapshotTypeRegistry`  | `[SnapshotName("ORDER", "AGGREGATE", "STATE", version: 1)]`      |
| Commands  | `[CommandName]`   | `ICommandTypeRegistry`   | `[CommandName("ORDER", "ACTIONS", "CREATE", version: 1)]`        |
| Messages  | `[MessageName]`   | `IMessageTypeRegistry`   | `[MessageName("ORDER", "NOTIFICATIONS", "ALERT", version: 1)]`   |

The same principles apply:

- Attribute provides stable storage identity with explicit `version` parameter
- Code identity (class name) is free to change
- `version` parameter enables schema evolution (increment, don't change name)
- Type-specific registry provides cached bidirectional lookup

## Anti-Patterns to Avoid

### ❌ Storing CLR Type Names

```csharp
// BAD: Tight coupling to code structure
eventType: "MyApp.Orders.Events.OrderShippedEvent, MyApp.Orders"
```

### ❌ Changing AppName/ModuleName/Name Values

```csharp
// BAD: Breaks deserialization of stored events
[EventName("ORDER", "SHIPPING", "COMPLETED", version: 1)]  // Was "FULFILLMENT", "SHIPPED"!
public sealed record OrderShippedEvent(...);
```

### ❌ Forgetting the Attribute

```csharp
// BAD: Will fail at startup validation
public sealed record OrderShippedEvent(...);  // Missing [EventName]!
```

### ❌ Duplicating Storage Names

```csharp
// BAD: Ambiguous - which type should be used?
[EventName("ORDER", "FULFILLMENT", "SHIPPED", version: 1)]
public sealed record OrderShippedEvent(...);

[EventName("ORDER", "FULFILLMENT", "SHIPPED", version: 1)]  // Duplicate!
public sealed record ShipmentCompletedEvent(...);
```

### ❌ Embedding Version in the Name String

```csharp
// BAD: Version should be the int parameter, not part of the name
[EventName("ORDER", "FULFILLMENT", "SHIPPEDV1", version: 1)]  // "V1" in name AND version: 1
public sealed record OrderShippedEvent(...);  // Results in ORDER.FULFILLMENT.SHIPPEDV1.V1

// GOOD: Keep name clean, use version parameter
[EventName("ORDER", "FULFILLMENT", "SHIPPED", version: 1)]  // Results in ORDER.FULFILLMENT.SHIPPED.V1
public sealed record OrderShippedEvent(...);
```

## Developer Experience Benefits

1. **Fearless Refactoring**: Rename, move, reorganize—storage doesn't care
2. **Fast Development**: No migrations for code structure changes
3. **Clear Contracts**: Attribute names are explicit, version-tracked contracts
4. **Startup Validation**: Missing/duplicate attributes caught early
5. **Performance**: Cached lookups, compiled deserializers, no runtime reflection
6. **Debugging**: Human-readable names in logs and storage browsers

## External References

- [Event Sourcing – Martin Fowler](https://martinfowler.com/eaaDev/EventSourcing.html)
- [Versioning in an Event Sourced System – Greg Young](https://leanpub.com/esversioning)
- [Schema Evolution in Event-Sourced Systems](https://www.eventstore.com/blog/event-versioning)
