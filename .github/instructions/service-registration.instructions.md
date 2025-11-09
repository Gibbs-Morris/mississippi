---
applyTo: "**/*ServiceRegistration*.cs,**/*DependencyInjection*.cs"
---

# Service Registration Pattern

## Scope
DI extension methods, Options pattern, hierarchical registration. DI property pattern in C# instructions.

## Quick-Start
```csharp
public static class ServiceRegistration {
  public static IServiceCollection AddFeature(this IServiceCollection svc, Action<FeatureOptions>? cfg = null) {
    if (cfg != null) svc.Configure(cfg);
    return svc.AddScoped<IFeatureService, FeatureService>();
  }
}
// ✅ GOOD: services.AddFeature(o => o.BatchSize = 100);
// ❌ BAD: new FeatureService(batchSize: 100) // no config param in ctor
```

## Core Principles
Private core method (no config params), public overloads for `Action<TOptions>`, `IConfiguration`, explicit params. Options pattern MUST be used; no direct ctor config params. Extension methods: `Add{Feature}()`. Public at product boundaries, internal for implementation details.

## Hierarchical Registration
Parent features call child registrations. `internal` for sub-features, `public` for logical boundaries.

## Async Initialization
Service registration MUST be synchronous. Use `IHostedService` for async DB init, Orleans lifecycle participants for grain setup, or factory patterns for deferred async.

## Options Validation
Use `ValidateOnStart()`, `IValidateOptions<T>`. Provide defaults via property initializers.

## Anti-Patterns
❌ Async registration methods. ❌ Config params in ctors. ❌ Hard-coded values. ❌ Public registration for internal features.

## Enforcement
Code reviews verify: Options pattern usage, hierarchical structure, synchronous methods, validation present. Connection strings via factories.
