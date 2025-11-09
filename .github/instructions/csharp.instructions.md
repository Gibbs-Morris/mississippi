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

## Access Control
Classes sealed unless inheritance justified. Interfaces internal unless public API. Grain interfaces public when external, internal otherwise. Document public/unsealed decisions in XML.

## Twelve-Factor (Cloud-Native)
Store config in env. Stateless processes. Fast startup/shutdown. Dev-prod parity. Logs as streams.

## Anti-Patterns
❌ Inheritance without justification. ❌ Public members unnecessarily. ❌ Config params in ctors. ❌ Mutable shared state. ❌ Blocking async with `.Result`. ❌ `Parallel.ForEach` in Orleans.

## Enforcement
Code reviews: SOLID adherence, access control justified, Options pattern, DI properties, no analyzer suppressions.
