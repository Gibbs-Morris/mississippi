---
applyTo: "**/*LoggerExtensions*.cs,**/Logging/**/*.cs"
---

# Logging Standards

## Scope
LoggerMessage patterns and `[ComponentName]LoggerExtensions` classes only. DI property pattern covered in C# instructions.

## Quick-Start
```csharp
public static class OrderLoggerExtensions {
  private static readonly Action<ILogger, string, Exception?> s_created =
    LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, "OrderCreated"), "Order {Id} created");
  public static void OrderCreated(this ILogger<OrderService> log, string id) => s_created(log, id, null);
}
// ✅ GOOD: Logger.OrderCreated(orderId);
// ❌ BAD: Logger.LogInformation("Order {Id} created", orderId);
```

## Core Principles
Extensions MUST: class per component, name ends with `LoggerExtensions`, all methods static, cached `Action` delegates. Direct `ILogger.Log*` MUST NOT appear. Inject as property: `private ILogger<T> Logger { get; }`.

## When to Log (MUST)
Public APIs (entry/exit/timing), exceptions, data mutations (CRUD), external calls, grain lifecycle, rule violations, event sourcing, >1s ops, >10MB allocations.

## Log Levels
Error: exceptions, failures. Warning: recoverable, deprecated. Information: lifecycle, business events. Debug: detailed flow, params. Trace: iterations, allocations.

## Structured Data
Use named params, not string interpolation. Include correlation IDs. Avoid PII.

## Anti-Patterns
❌ Direct ILogger calls. ❌ String concatenation. ❌ Sensitive data. ❌ Blocking calls in log methods.

## Enforcement
Code reviews verify: extension classes exist, no direct calls, structured params, correlation IDs, appropriate levels. See scratchpad tasks for detected violations.
