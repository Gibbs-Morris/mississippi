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

## Registration Pattern

### Method Structure
```csharp
// Private core - no configuration parameters
private static IServiceCollection AddFeatureCore(this IServiceCollection services) {
  return services.AddScoped<IFeatureService, FeatureService>();
}

// Public overloads
public static IServiceCollection AddFeature(this IServiceCollection services, Action<FeatureOptions>? configure = null) {
  if (configure != null) services.Configure(configure);
  return services.AddFeatureCore();
}

public static IServiceCollection AddFeature(this IServiceCollection services, IConfiguration config) {
  services.Configure<FeatureOptions>(config);
  return services.AddFeatureCore();
}
```

### Hierarchical Registration
Parent features call child registrations. Use `internal` for sub-features, `public` for logical product boundaries.

```csharp
public static IServiceCollection AddParentFeature(this IServiceCollection svc, Action<ParentOptions>? cfg = null) {
  if (cfg != null) svc.Configure(cfg);
  return svc.AddChildFeature()  // Calls child registration
             .AddScoped<IParentService, ParentService>();
}
```

## Options Pattern

### Configuration Requirements
Services MUST accept IOptions<T>, NOT config values directly in constructor. Options classes provide defaults via property initializers.

### Validation
Use `ValidateOnStart()` to fail fast. Implement `IValidateOptions<T>` for complex validation.

```csharp
services.AddOptions<FeatureOptions>()
        .Bind(configuration.GetSection("Feature"))
        .ValidateOnStart();
```

## Async Initialization
Service registration MUST be synchronous. Use `IHostedService` for async DB init, Orleans lifecycle participants for grain setup, or factory patterns for deferred async work.

## Anti-Patterns
❌ Async registration methods. ❌ Config params in ctors. ❌ Hard-coded values. ❌ Public registration for internal features.

## Enforcement
Code reviews verify: Options pattern usage, hierarchical structure, synchronous methods, validation present. Connection strings via factories.
