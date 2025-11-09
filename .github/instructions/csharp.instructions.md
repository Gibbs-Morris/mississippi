---
applyTo: "**/*.cs"
---

# C# General Standards

## Scope
Language features, SOLID, immutability, access control. Orleans/logging/DI registration in dedicated files.

## Quick-Start
```csharp
public sealed class OrderProcessor {
  private ILogger<OrderProcessor> Logger { get; }
  private IOptions<OrderOptions> Options { get; }
  public OrderProcessor(ILogger<OrderProcessor> log, IOptions<OrderOptions> opts) {
    Logger = log; Options = opts;
  }
}
// ✅ GOOD: sealed, property DI, Options pattern
// ❌ BAD: public class with fields, config params in ctor
```

## Core Principles
SOLID MUST apply. Unit-testable via DI. Sealed by default; composition over inheritance. Members private by default; internal for assembly sharing. DI via `private Type Name { get; }`. Options pattern MUST be used. Immutable preferred: records, `init`. Analyzers MUST stay enabled. Orleans: no `Parallel.ForEach`, use `Task.WhenAll`.

## SOLID Guidelines

### Single Responsibility
Each class has one reason to change. Validator validates, processor processes, repository persists - never combined.

### Open/Closed
Extend via composition and interfaces, not inheritance. Use strategy pattern, decorator pattern.

### Liskov Substitution
Subtypes MUST be substitutable for base types. If inheritance is used, derived classes MUST honor base class contracts.

### Interface Segregation
Many small, focused interfaces over large general ones. Clients SHOULD NOT depend on methods they don't use.

### Dependency Inversion
Depend on abstractions (interfaces), not concrete types. High-level modules MUST NOT depend on low-level modules.

## Access Control

### Defaults
Classes: sealed unless inheritance justified. Members: private unless sharing needed. Interfaces: internal unless public API.

### Public APIs
Public/protected/unsealed classes MUST have documented justification in XML comments explaining the extension point.

### Orleans Grain Interfaces
Public when consumed by external clients. Internal when used only intra-assembly.

## Dependency Injection
Use `private Type Name { get; }` pattern for all injected dependencies. Constructor-only injection. No field injection.

## Configuration
Options pattern (IOptions<T>, IOptionsSnapshot<T>, IOptionsMonitor<T>) MUST be used. NEVER pass configuration values as constructor parameters.

## Immutability
Prefer records or init-only properties for domain models, events, public contracts. Relax only with clear justification.

## Twelve-Factor (Cloud-Native)
Store config in env. Stateless processes. Fast startup/shutdown. Dev-prod parity. Logs as streams. Backing services as attached resources.

## Orleans-Specific
NEVER use Parallel.ForEach (violates single-threaded model). Use await + Task.WhenAll for fan-out concurrency. Avoid blocking calls. No shared mutable state.

## Anti-Patterns
❌ Inheritance without justification. ❌ Public members unnecessarily. ❌ Config params in ctors. ❌ Mutable shared state. ❌ Blocking async with `.Result`. ❌ `Parallel.ForEach` in Orleans.

## Enforcement
Code reviews: SOLID adherence, access control justified, Options pattern, DI properties, no analyzer suppressions.
