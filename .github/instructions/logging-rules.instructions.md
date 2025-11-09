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

## When to Add Logging

### ALWAYS Log (MUST)
1. Method is public and part of a service interface
2. Method can throw an exception
3. Method performs I/O operations (database, file, network)
4. Method modifies data (create, update, delete)
5. Method calls external services
6. Method is an Orleans grain method
7. Method implements business logic with rules/validation
8. Method takes longer than 1 second to execute
9. Method allocates significant resources (>10MB memory, database connections)
10. Method is part of the event sourcing pipeline

### Consider Logging (SHOULD)
- Method is complex with multiple decision points
- Method is frequently called and performance matters
- Method handles user input or external data
- Method is difficult to test or debug

### Usually Don't Log
- Simple getters/setters
- Constructors (unless they do significant work)
- Private helper methods that are single-purpose
- Property accessors
- Methods that only transform data without side effects

## Log Levels
Error: unhandled exceptions, system failures, external service failures that impact functionality.  
Warning: recoverable errors, deprecated feature usage, configuration issues with fallbacks.  
Information: business events, service lifecycle (startup/shutdown), performance metrics, significant user actions.  
Debug: detailed execution flow, method entry/exit with parameters, intermediate calculation results.  
Trace: loop iterations, memory allocations, network call details.

## Structured Data
Use named params, not string interpolation. Include correlation IDs. Avoid PII.

## Anti-Patterns
❌ Direct ILogger calls. ❌ String concatenation. ❌ Sensitive data. ❌ Blocking calls in log methods.

## Enforcement
Code reviews verify: extension classes exist, no direct calls, structured params, correlation IDs, appropriate levels. See scratchpad tasks for detected violations.
