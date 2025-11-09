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

## Lifecycle
`OnActivateAsync`, `OnDeactivateAsync`. Use `IRemindable` for reminders, `RegisterGrainTimer` for timers.

## Anti-Patterns
❌ Inheriting from `Grain`. ❌ `Parallel.ForEach`. ❌ Blocking calls. ❌ Shared mutable state.

## Enforcement
Code reviews: POCO pattern, sealed classes, extension methods used, no `Parallel.ForEach`.
