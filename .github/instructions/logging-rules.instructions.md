---
applyTo: '**/*.cs'
---

# Enterprise Logging Best Practices

Governing thought: Log through cached LoggerExtensions helpers so every entry is structured, correlated, and zero-allocation.

## Rules (RFC 2119)

- Pattern: define one `public static class <Component>LoggerExtensions` per area; cache messages with `LoggerMessage.Define` (or source-gen equivalents) and expose only extension methods. Direct `ILogger.Log*` **MUST NOT** be used. LoggerExtensions classes **MUST** end with `LoggerExtensions`.
- Dependency injection **MUST** use `private ILogger<T> Logger { get; }` and other services as get-only properties; no globals. Check log level before expensive formatting.
- Mandatory coverage: log public service/grain/page entry and successful completion with timing; every catch block with context; CRUD operations; external/infra calls (start+result); Orleans activation/deactivation and grain method calls; event append/read; batch operations with counts; operations running >1s or notable allocations (>10MB); business rule violations. Grain helpers **MUST** use Orleans RequestContext for correlation and timers/reminders when relevant.
- Sensitive data **MUST NOT** be logged; mask tokens/PII. Include correlation/request IDs wherever possible and prefer structured properties over string concatenation. Skip trivial getters/setters and pure transforms to avoid noise.
- When direct `ILogger` calls or missing required logs are found, agents **MUST** open a scratchpad task and migrate to the pattern.
- Log levels **SHOULD** reflect severity (errors for failures, warnings for degraded states, info for lifecycle/business events, debug/trace for detailed flow). Messages **SHOULD** stay short, domain-specific, and AI-readable.

## Scope and Audience

Applies to all C# code in Mississippi projects and samples.

## Quick Start

- Create `ComponentLoggerExtensions` with cached delegates and extension methods.
- Inject `ILogger<T>` via property pattern; call extensions for every required scenario above.
- Propagate/emit correlation IDs (HTTP headers → RequestContext) and avoid logging secrets.

## Review Checklist

- [ ] All logging goes through LoggerExtensions with cached templates.
- [ ] Required scenarios (entry/exit, catch, CRUD, external calls, long ops, business rules, grains/event sourcing/batches) are covered with structured data and correlation IDs.
- [ ] No PII/tokens logged; noise kept low; levels appropriate.
- [ ] Orleans code uses RequestContext for correlation and logs lifecycle/method timing.
- [ ] Direct `ILogger` calls replaced or tracked in scratchpad.
