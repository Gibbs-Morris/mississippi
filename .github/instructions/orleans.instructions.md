---
applyTo: "**/Orleans/**/*.cs,**/*Grain*.cs"
---

# Orleans Standards

## Scope
Grain patterns, lifecycle. Serialization in separate file. POCO pattern MUST be used.

## Quick-Start
```csharp
public sealed class OrderGrain : IGrainBase, IOrderGrain {
  public IGrainContext GrainContext { get; }
  private ILogger<OrderGrain> Logger { get; }
  public OrderGrain(IGrainContext ctx, ILogger<OrderGrain> log) {
    GrainContext = ctx; Logger = log;
  }
}
// ✅ GOOD: sealed, IGrainBase, inject IGrainContext
// ❌ BAD: inheriting from Grain class
```

## Core Principles
NEVER inherit from `Grain`. Use `IGrainBase` with `IGrainContext`. Grain classes MUST be sealed. Extension methods: `this.GetPrimaryKey()`, `this.DeactivateOnIdle()`, etc. DI via properties. State: `IPersistentState<T>` injected.

## Grain Pattern

### Structure
```csharp
public sealed class OrderGrain : IGrainBase, IOrderGrain {
  public IGrainContext GrainContext { get; }
  private ILogger<OrderGrain> Logger { get; }
  private IPersistentState<OrderState> State { get; }
  
  public OrderGrain(IGrainContext ctx, ILogger<OrderGrain> log, 
                    [PersistentState("order")] IPersistentState<OrderState> state) {
    GrainContext = ctx; Logger = log; State = state;
  }
  
  public Task OnActivateAsync(CancellationToken ct) => Task.CompletedTask;
  public Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct) => Task.CompletedTask;
}
```

### Extension Methods
Use extension methods for grain context operations: `this.GetPrimaryKey()`, `this.GetPrimaryKeyString()`, `this.DeactivateOnIdle()`, `this.DelayDeactivation(TimeSpan)`.

### State Management
Inject `IPersistentState<T>` with `[PersistentState("stateName")]` attribute. Call `State.WriteStateAsync()` to persist. State loaded automatically on activation.

## Lifecycle
`OnActivateAsync`: called when grain activates. `OnDeactivateAsync`: called before deactivation. Use `IRemindable` for durable reminders. Use `RegisterGrainTimer` for in-memory timers (lost on deactivation).

## Concurrency
Orleans guarantees single-threaded execution per grain activation. NEVER use `Parallel.ForEach` - violates grain model. Use `await Task.WhenAll()` for concurrent operations. Avoid blocking calls (`.Result`, `.Wait()`).

## Anti-Patterns
❌ Inheriting from `Grain`. ❌ `Parallel.ForEach`. ❌ Blocking calls. ❌ Shared mutable state. ❌ Chatty inter-grain calls.

## Enforcement
Code reviews: POCO pattern, sealed classes, extension methods used, no `Parallel.ForEach`, async properly awaited.
