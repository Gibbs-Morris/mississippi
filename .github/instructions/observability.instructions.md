---
applyTo: '**/*.cs'
---

# Observability and Telemetry Best Practices

Governing thought: Use .NET built-in telemetry primitives (ActivitySource, Meter) with OpenTelemetry-compatible patterns to provide low-overhead, vendor-neutral observability that correlates logs, traces, and metrics across the Mississippi Framework.

## Rules (RFC 2119)

### General Principles

- All telemetry **MUST** use .NET built-in types: `ActivitySource`, `Activity`, `Meter`, `Counter<T>`, `Histogram<T>`, and `ILogger`.  
  Why: Built-in types are OpenTelemetry-compatible without vendor lock-in.
- Telemetry **MUST** be low-allocation and safe under high throughput.  
  Why: Framework code runs in hot paths; allocations impact GC and latency.
- `ActivitySource` and `Meter` instances **MUST** be declared as `static readonly` fields.  
  Why: These are designed for reuse; creating per-call causes memory leaks.
- Secrets, PII, and high-cardinality identifiers **MUST NOT** be logged, traced, or added as metric tags.  
  Why: Protects user privacy and prevents metric cardinality explosion.
- All telemetry **MUST** be optional and safe when collectors are not configured.  
  Why: Framework consumers may not enable telemetry; code must not fail.

### ActivitySource and Tracing

- Each project **MUST** define at most one `ActivitySource` with name matching pattern `Mississippi.{ComponentName}`.  
  Why: Enables selective listener subscription per component.
- Activity names **MUST** use PascalCase verb-noun format: `{Verb}{Noun}` (e.g., `ExecuteCommand`, `AppendEvents`).  
  Why: Consistent naming improves trace readability and filtering.
- `StartActivity()` **MUST** be guarded with null checks since it returns `null` when no listener is attached.  
  Why: Prevents NullReferenceException in production when tracing is disabled.
- Activity tags **MUST** use lowercase dot-separated names following OpenTelemetry semantic conventions (e.g., `mississippi.brook.name`, `messaging.operation`).  
  Why: Consistency with OTel conventions enables cross-system correlation.
- High-cardinality values (user IDs, request IDs, order IDs) **MUST NOT** be added as activity tags.  
  Why: Causes cardinality explosion in tracing backends.
- Tags **SHOULD** only be added when `Activity.IsAllDataRequested` is true.  
  Why: Avoids unnecessary allocations when tracing is sampled out.
- Error recording **MUST** use `Activity.SetStatus(ActivityStatusCode.Error)` and `Activity.RecordException()`.  
  Why: Standard error recording enables unified error dashboards.
- Activity creation **SHOULD** use `ActivityKind.Internal` for framework operations, `ActivityKind.Client` for outbound calls.  
  Why: Correct kind enables proper span visualization in trace UIs.

### Meter and Metrics

- Each project **MUST** define at most one `Meter` with name matching pattern `Mississippi.{ComponentName}`.  
  Why: Enables selective metric collection per component.
- Metric names **MUST** use lowercase dot-separated format: `mississippi.{component}.{metric}` (e.g., `mississippi.aggregates.commands_executed`).  
  Why: Consistent naming across all framework metrics.
- Metric units **MUST** follow OpenTelemetry conventions: use `s` for seconds, `ms` for milliseconds, `By` for bytes.  
  Why: Enables automatic unit conversion in observability tools.
- Counter metrics **SHOULD** use suffix `_total` for monotonic counters.  
  Why: Prometheus convention for identifying counter metrics.
- Histogram metrics **SHOULD** use suffix indicating what is measured (e.g., `_duration_ms`, `_size_bytes`).  
  Why: Self-documenting metric names.
- Metric tags **MUST** have bounded cardinality (≤100 distinct values).  
  Why: Unbounded cardinality causes memory issues in metrics backends.
- Allowed metric tags: `brook.name`, `aggregate.type`, `command.type`, `event.type`, `error.code`, `status`.  
  Why: Low-cardinality categorical values that aid filtering.
- Forbidden metric tags: user IDs, session IDs, request IDs, correlation IDs, entity IDs.  
  Why: High-cardinality values cause metric explosion.
- Observable instruments (gauges) **SHOULD** be used for values that are already computed (queue depth, cache size).  
  Why: Avoids duplicate computation; values are pulled on scrape.

### Logging Integration

- Log messages **MUST** correlate with traces via automatic trace/span ID injection.  
  Why: Enables jumping from logs to traces in observability tools.
- Logging **MUST** follow existing `LoggerExtensions` pattern with `[LoggerMessage]` source generators.  
  Why: Consistency with established logging patterns; zero-allocation.
- Trace and span IDs **SHOULD NOT** be manually logged; rely on log provider correlation.  
  Why: Reduces duplication; modern log providers inject automatically.

### Registration and Configuration

- Telemetry registration **MUST** be provided via extension methods on `IServiceCollection`.  
  Why: Follows established DI patterns in the framework.
- Registration methods **MUST** be named `AddMississippiObservability()` or `AddMississippi{Component}Telemetry()`.  
  Why: Consistent naming with existing registration patterns.
- Telemetry **MUST** be opt-in; no telemetry collection without explicit registration.  
  Why: Framework consumers control their observability stack.
- Consumer apps **SHOULD** use `AddOpenTelemetry()` from Microsoft.Extensions.* packages to configure exporters.  
  Why: Separates framework instrumentation from export configuration.

## Scope and Audience

**Audience:** All developers instrumenting Mississippi Framework components or consuming the framework in applications.

**In scope:** ActivitySource usage, Meter usage, metric naming, tag conventions, logging correlation, registration patterns.

**Out of scope:** Exporter configuration, vendor-specific setup, log aggregation infrastructure.

## Purpose

This document establishes observability standards ensuring consistent, low-overhead telemetry across the Mississippi Framework that integrates seamlessly with OpenTelemetry-compatible collectors.

## Core Principles

- Use .NET built-in telemetry types (no direct OTel SDK dependencies in core libraries)
- Keep framework telemetry low-overhead and allocation-free in hot paths
- Ensure telemetry is vendor-neutral and OTLP-exportable
- Correlate logs, traces, and metrics through standard mechanisms
- Make telemetry opt-in with safe defaults when disabled

## ActivitySource Patterns

### Declaring an ActivitySource

```csharp
namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
/// Provides telemetry instrumentation for aggregate operations.
/// </summary>
internal static class AggregatesTelemetry
{
    /// <summary>
    /// The name of the ActivitySource for aggregate operations.
    /// </summary>
    public const string ActivitySourceName = "Mississippi.Aggregates";

    /// <summary>
    /// The version of the telemetry instrumentation.
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// ActivitySource for tracing aggregate operations.
    /// </summary>
    public static readonly ActivitySource Source = new(ActivitySourceName, Version);
}
```

### Creating Activities Safely

```csharp
public async Task<OperationResult> ExecuteAsync<TCommand>(TCommand command)
{
    // StartActivity returns null when no listener is attached
    using Activity? activity = AggregatesTelemetry.Source.StartActivity(
        "ExecuteCommand",
        ActivityKind.Internal);

    // Only add tags when tracing is enabled and data is requested
    if (activity?.IsAllDataRequested == true)
    {
        activity.SetTag("mississippi.aggregate.type", typeof(TSnapshot).Name);
        activity.SetTag("mississippi.command.type", typeof(TCommand).Name);
    }

    try
    {
        OperationResult result = await ExecuteInternalAsync(command);

        activity?.SetTag("mississippi.operation.status", result.Success ? "success" : "failed");

        if (!result.Success)
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
        }

        return result;
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.RecordException(ex);
        throw;
    }
}
```

### Activity Naming Conventions

| Operation | Activity Name | Kind |
|-----------|--------------|------|
| Execute command | `ExecuteCommand` | Internal |
| Append events | `AppendEvents` | Internal |
| Read events | `ReadEvents` | Internal |
| Reduce projection | `ReduceProjection` | Internal |
| Load snapshot | `LoadSnapshot` | Internal |
| Save snapshot | `SaveSnapshot` | Internal |
| Cosmos DB read | `CosmosRead` | Client |
| Cosmos DB write | `CosmosWrite` | Client |

## Meter Patterns

### Declaring a Meter

```csharp
namespace Mississippi.EventSourcing.Aggregates;

/// <summary>
/// Provides metrics instrumentation for aggregate operations.
/// </summary>
internal static class AggregatesMetrics
{
    /// <summary>
    /// The name of the Meter for aggregate metrics.
    /// </summary>
    public const string MeterName = "Mississippi.Aggregates";

    /// <summary>
    /// Meter for aggregate operation metrics.
    /// </summary>
    public static readonly Meter Meter = new(MeterName, "1.0.0");

    /// <summary>
    /// Counter for commands executed.
    /// </summary>
    public static readonly Counter<long> CommandsExecuted = Meter.CreateCounter<long>(
        "mississippi.aggregates.commands_executed_total",
        unit: "{command}",
        description: "Total number of commands executed against aggregates");

    /// <summary>
    /// Histogram for command execution duration.
    /// </summary>
    public static readonly Histogram<double> CommandDuration = Meter.CreateHistogram<double>(
        "mississippi.aggregates.command_duration_ms",
        unit: "ms",
        description: "Duration of command execution in milliseconds");

    /// <summary>
    /// Counter for events appended.
    /// </summary>
    public static readonly Counter<long> EventsAppended = Meter.CreateCounter<long>(
        "mississippi.aggregates.events_appended_total",
        unit: "{event}",
        description: "Total number of events appended to brooks");

    /// <summary>
    /// Counter for command failures.
    /// </summary>
    public static readonly Counter<long> CommandFailures = Meter.CreateCounter<long>(
        "mississippi.aggregates.command_failures_total",
        unit: "{command}",
        description: "Total number of failed command executions");
}
```

### Recording Metrics

```csharp
public async Task<OperationResult> ExecuteAsync<TCommand>(TCommand command)
{
    long startTimestamp = Stopwatch.GetTimestamp();
    string commandType = typeof(TCommand).Name;

    try
    {
        OperationResult result = await ExecuteInternalAsync(command);

        // Record duration
        double elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        AggregatesMetrics.CommandDuration.Record(
            elapsedMs,
            new KeyValuePair<string, object?>("command.type", commandType),
            new KeyValuePair<string, object?>("status", result.Success ? "success" : "failed"));

        // Record success/failure counter
        if (result.Success)
        {
            AggregatesMetrics.CommandsExecuted.Add(
                1,
                new KeyValuePair<string, object?>("command.type", commandType));
        }
        else
        {
            AggregatesMetrics.CommandFailures.Add(
                1,
                new KeyValuePair<string, object?>("command.type", commandType),
                new KeyValuePair<string, object?>("error.code", result.ErrorCode));
        }

        return result;
    }
    catch (Exception)
    {
        AggregatesMetrics.CommandFailures.Add(
            1,
            new KeyValuePair<string, object?>("command.type", commandType),
            new KeyValuePair<string, object?>("error.code", "unhandled_exception"));
        throw;
    }
}
```

### Metric Naming Conventions

| Metric | Name | Unit | Type |
|--------|------|------|------|
| Commands executed | `mississippi.aggregates.commands_executed_total` | `{command}` | Counter |
| Command duration | `mississippi.aggregates.command_duration_ms` | `ms` | Histogram |
| Events appended | `mississippi.brooks.events_appended_total` | `{event}` | Counter |
| Events read | `mississippi.brooks.events_read_total` | `{event}` | Counter |
| Snapshot loads | `mississippi.snapshots.loads_total` | `{snapshot}` | Counter |
| Snapshot saves | `mississippi.snapshots.saves_total` | `{snapshot}` | Counter |

## Consumer Registration

### Application Setup

```csharp
// In Program.cs or Startup.cs
var builder = Host.CreateApplicationBuilder(args);

// Register Mississippi framework telemetry
builder.Services.AddMississippiObservability();

// Configure OpenTelemetry export (consumer responsibility)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("Mississippi.Aggregates")
            .AddSource("Mississippi.Brooks")
            .AddSource("Mississippi.Reducers")
            .AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("Mississippi.Aggregates")
            .AddMeter("Mississippi.Brooks")
            .AddMeter("Mississippi.Reducers")
            .AddOtlpExporter();
    });
```

### Selective Instrumentation

```csharp
// Only enable tracing for specific components
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("Mississippi.Aggregates")  // Only aggregate tracing
            .AddOtlpExporter();
    });
```

## Validation and Testing

### Local Validation

To validate telemetry emission locally:

1. Use the console exporter during development:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("Mississippi.Aggregates")
            .AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("Mississippi.Aggregates")
            .AddConsoleExporter();
    });
```

2. Use OTLP with a local collector (Jaeger, Aspire Dashboard):

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("Mississippi.Aggregates")
            .AddOtlpExporter(opts => opts.Endpoint = new Uri("http://localhost:4317"));
    });
```

### Unit Testing Telemetry

```csharp
[Fact]
public void ExecuteCommandCreatesActivityWithTags()
{
    // Arrange
    using var listener = new ActivityListener
    {
        ShouldListenTo = source => source.Name == "Mississippi.Aggregates",
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        ActivityStarted = activity => Assert.NotNull(activity),
    };
    ActivitySource.AddActivityListener(listener);

    // Act
    // Execute command...

    // Assert
    // Verify activity was created with expected tags
}
```

### Cardinality Validation

Before adding a new metric tag, verify:

1. **Bounded values**: Can you list all possible values? If not, don't add it.
2. **Maximum distinct values**: Will there be ≤100 distinct values in production?
3. **Static or slow-changing**: Does the value change rarely (e.g., per deployment, not per request)?

## PR Checklist

When adding new telemetry:

- [ ] ActivitySource/Meter declared as `static readonly`
- [ ] Activity/metric names follow naming conventions
- [ ] `StartActivity()` result checked for null
- [ ] Tags only added when `IsAllDataRequested` is true
- [ ] No high-cardinality tags (user IDs, request IDs, etc.)
- [ ] No PII or secrets in tags or logs
- [ ] Metrics use correct units (`ms`, `s`, `By`, etc.)
- [ ] Counter metrics use `_total` suffix
- [ ] Error recording uses `SetStatus` and `RecordException`
- [ ] Tests verify telemetry emission

When adding new public APIs:

- [ ] Activity created for the operation
- [ ] Duration histogram recorded
- [ ] Success/failure counter recorded

## Anti-Patterns to Avoid

### ❌ Creating ActivitySource Per Call

```csharp
// BAD: Creates new ActivitySource on every call - memory leak!
public async Task DoWorkAsync()
{
    using var source = new ActivitySource("Mississippi.Component");
    using var activity = source.StartActivity("DoWork");
    // ...
}
```

### ❌ Not Checking for Null Activity

```csharp
// BAD: NullReferenceException when no listener attached
using Activity? activity = Source.StartActivity("DoWork");
activity.SetTag("key", "value");  // Throws if activity is null!
```

### ❌ High-Cardinality Tags

```csharp
// BAD: User ID has unbounded cardinality
activity?.SetTag("user.id", userId);
CommandsExecuted.Add(1, new KeyValuePair<string, object?>("user.id", userId));
```

### ❌ Logging Secrets

```csharp
// BAD: Token is a secret!
Logger.LogInformation("Authenticated with token {Token}", authToken);
```

### ❌ Unconditional Tag Allocation

```csharp
// BAD: Allocates even when activity is sampled out
activity?.SetTag("details", JsonSerializer.Serialize(complexObject));

// GOOD: Check IsAllDataRequested first
if (activity?.IsAllDataRequested == true)
{
    activity.SetTag("details", JsonSerializer.Serialize(complexObject));
}
```

## External References

- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
- [.NET ActivitySource API](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs)
- [.NET Metrics API](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation)
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)

---
Last verified: 2025-12-18
Default branch: main
