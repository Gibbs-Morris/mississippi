# Design Review: Architect Perspective

**Reviewer Role:** Software Architect (focused on SOLID, DRY, KISS, avoiding overengineering)  
**Date:** 2026-01-24  
**RFC:** Server-Side Event Effects

---

## Overall Assessment: ⚠️ Cautiously Positive

The design is sound at its core, but there are areas where it risks overengineering. Some components could be simplified or deferred. Let me break down by principle.

---

## SOLID Analysis

### Single Responsibility Principle ✅

**Good:** Each component has one job:
- `IEventEffect<TEvent, TAggregate>` → Handle one event type
- `EffectDispatcherGrain` → Dispatch effects to handlers
- `EffectRegistry` → Resolve effects by aggregate type
- `AggregateCommandGateway` → Send commands from effects

**Concern:** `EffectRegistry` does two things:
1. Type name → Type resolution (via reflection)
2. Type → `IEnumerable<IEventEffect<>>` resolution (via DI)

**Recommendation:** Split these if they change for different reasons.

### Open/Closed Principle ✅

**Good:** The design is extensible without modification:
- New effects are added as new classes (no core changes)
- Generator discovers them automatically
- `IEffectRegistry` is injectable for custom implementations

### Liskov Substitution Principle ✅

**Good:** `IEventEffect<TEvent, TAggregate>` and `EventEffectBase` maintain proper substitutability. The typed interface properly extends the base interface.

### Interface Segregation Principle ⚠️

**Concern:** `IEventEffect<TAggregate>` forces implementers to have:
```csharp
bool CanHandle(object eventData);
Task HandleAsync(object eventData, ...);
```

But `EventEffectBase` already handles the `object` → typed dispatch. Most users will never implement `IEventEffect<TAggregate>` directly.

**Question:** Do we need the non-generic interface at all? It exists for dispatch, but could be internal.

**Recommendation:** Consider making `IEventEffect<TAggregate>` internal and only exposing `IEventEffect<TEvent, TAggregate>` + `EventEffectBase`.

### Dependency Inversion Principle ✅

**Good:** 
- High-level policy (aggregate) depends on abstraction (`IEffectDispatcherGrain`)
- Effects depend on abstractions (`IAggregateCommandGateway`, `IEmailService`, etc.)
- No concrete dependencies leak into interfaces

---

## DRY Analysis

### Duplication Risk 1: Registration Pattern

The design proposes:
```csharp
services.AddEventEffect<AccountOpened, BankAccountAggregate, AccountOpenedEffect>();
```

This mirrors:
```csharp
services.AddCommandHandler<OpenAccount, BankAccountAggregate, OpenAccountHandler>();
services.AddReducer<AccountOpened, BankAccountAggregate, AccountOpenedReducer>();
```

**Observation:** Three near-identical registration methods. The generator hides this, but the underlying pattern is repeated.

**Verdict:** Acceptable—the semantic differences (handler vs reducer vs effect) justify separate methods. The generator's existence makes this a non-issue for users.

### Duplication Risk 2: Base Class Pattern

`EventEffectBase` mirrors `CommandHandlerBase` and `EventReducerBase`. Similar patterns:
- Type checking via `is TEvent`
- Dispatch to typed method
- Generic type parameters for event and aggregate

**Verdict:** This is **intentional consistency**, not problematic duplication. The similarity is a feature.

---

## KISS Analysis

### Concern 1: IEffectRegistry is Overengineered 🔴

The `EffectRegistry` does runtime reflection:
```csharp
AppDomain.CurrentDomain.GetAssemblies()
    .SelectMany(a => a.GetTypes())
    .FirstOrDefault(t => t.FullName == name)
```

**Why is this complex?**
- Passing `aggregateTypeName` as string across grain boundary
- Reconstructing the type via reflection
- Building generic `IEventEffect<>` dynamically

**Simpler Alternative:** Use a keyed service pattern:
```csharp
// Registration (generated)
services.AddKeyedTransient<IEventEffect>("BankAccountAggregate", typeof(AccountOpenedEffect));

// Resolution (in EffectDispatcherGrain)
var effects = serviceProvider.GetKeyedServices<IEventEffect>(aggregateTypeName);
```

.NET 8's keyed services eliminate the reflection entirely.

**Recommendation:** Replace `IEffectRegistry` with keyed DI services. Removes a type, removes reflection, uses platform features.

### Concern 2: IAggregateCommandGateway for V1? 🟡

The gateway enables saga patterns, but:
- Sagas are explicitly "Future PR" scope
- The gateway adds API surface that must be maintained
- Users might misuse it before saga patterns are documented

**Question:** Should `IAggregateCommandGateway` be in V1 or wait for the saga PR?

**Recommendation:** Either:
- **Option A:** Include it now (if sagas are a known near-term need)
- **Option B:** Defer to saga PR (YAGNI—You Aren't Gonna Need It yet)

If included, mark it:
```csharp
/// <summary>
///     Gateway for saga patterns. See saga documentation for proper usage.
/// </summary>
/// <remarks>
///     <para>
///         WARNING: Using this gateway outside of saga patterns can lead to
///         circular dependencies and infinite loops. Use with caution.
///     </para>
/// </remarks>
public interface IAggregateCommandGateway { ... }
```

### Concern 3: Is StatelessWorker + [OneWay] the Right Abstraction? 🟡

The design uses a dedicated grain for dispatch:
```csharp
var dispatcher = GrainFactory.GetGrain<IEffectDispatcherGrain>(Guid.NewGuid().ToString());
_ = dispatcher.DispatchAsync(...);
```

**Trade-offs:**
- ✅ Isolates effect execution from aggregate grain
- ✅ Auto-scales via StatelessWorker
- ❌ Adds a grain hop (latency, serialization)
- ❌ Generates a new grain per dispatch (GUID key)

**Simpler Alternative for Simple Effects:**
```csharp
// In GenericAggregateGrain
_ = Task.Run(async () => 
{
    foreach (var effect in effects)
    {
        try { await effect.HandleAsync(...); }
        catch { /* log */ }
    }
});
```

**However:** The RFC correctly notes this loses Orleans context. The grain approach is right for distributed tracing and observability.

**Verdict:** Keep `EffectDispatcherGrain`, but document why (observability, cluster distribution) so it doesn't look overengineered.

---

## Overengineering Risk Assessment

| Component | Overengineering Risk | Verdict |
|-----------|---------------------|---------|
| `IEventEffect` interface | Low | Necessary abstraction |
| `EventEffectBase` | Low | Reduces boilerplate |
| `EffectDispatcherGrain` | Medium | Justified for isolation/observability |
| `IEffectRegistry` | High | **Replace with keyed services** |
| `IAggregateCommandGateway` | Medium | Consider deferring to saga PR |
| Source generator changes | Low | Follows established pattern |

---

## Architectural Recommendations

### 1. Simplify Effect Resolution

Replace:
```csharp
public interface IEffectRegistry
{
    IEnumerable<object> GetEffects(string aggregateTypeName);
}
```

With .NET 8 keyed services:
```csharp
// Generated registration
services.AddKeyedTransient<IEventEffect<BankAccountAggregate>, AccountOpenedEffect>(
    serviceKey: "Spring.Domain...BankAccountAggregate");

// Dispatcher resolution
var effects = serviceProvider.GetKeyedServices<IEventEffect<TAggregate>>(aggregateTypeName);
```

### 2. Defer IAggregateCommandGateway

Move to saga PR unless there's a concrete V1 use case. This follows YAGNI and reduces initial scope.

### 3. Add Architectural Decision Record (ADR)

Document why `EffectDispatcherGrain` was chosen over:
- Inline `Task.Run`
- Orleans Streams
- Outbox pattern

This prevents future engineers from thinking "this is overengineered" without understanding the trade-offs.

### 4. Consider Effect Categories

For future extensibility, consider categorizing effects:
```csharp
public enum EffectCategory
{
    Notification,    // Emails, SMS
    Integration,     // External APIs
    Audit,           // Logging, compliance
    Orchestration    // Saga steps
}
```

This allows per-category policies (e.g., "retry notifications but not audits").

---

## Verdict

**Approve with changes:**

1. **Required:** Replace `IEffectRegistry` with keyed services (simplification)
2. **Recommended:** Defer `IAggregateCommandGateway` to saga PR (scope reduction)
3. **Recommended:** Add ADR explaining grain choice over alternatives

The core design (`IEventEffect` + `EventEffectBase` + `EffectDispatcherGrain`) is solid and follows good principles. The registry abstraction is the main area of unnecessary complexity.
