---
applyTo: '**/*.cs'
---

# Enterprise Logging Best Practices

Governing thought: Use high-performance LoggerExtensions pattern with LoggerMessage for all logging to ensure consistency, observability, and zero-allocation performance across the enterprise.

## Rules (RFC 2119)

- All logging **MUST** use LoggerExtensions pattern with `public static class [ComponentName]LoggerExtensions` for each component.  
  Why: Ensures consistency and high performance through compile-time optimization.
- Direct ILogger calls **MUST NOT** be used; all logging **MUST** go through static extension methods.  
  Why: Enforces the LoggerExtensions pattern and prevents performance issues.
- LoggerExtensions class names **MUST** end with `LoggerExtensions` suffix.  
  Why: Maintains naming consistency across components.
- LoggerMessage patterns **MUST** use source generator attributes (`[LoggerMessage]`).  
  Why: Source generators provide identical performance with ~50% less boilerplate, following KISS and DRY principles.
- Each log operation **MUST** be exposed as a public static extension method.  
  Why: Provides consistent API for logging across components.
- Direct ILogger.Log() calls **MUST NOT** be used; always use LoggerMessage source generators.  
  Why: LoggerMessage provides pre-compiled message templates and optimal performance.
- Dependency-injected ILogger **MUST** use `private ILogger<T> Logger { get; }` property pattern.  
  Why: Encourages immutability and clarity of dependencies, consistent with DI best practices.
- All public service methods **MUST** log entry and successful completion.  
  Why: Provides observability for service operations and performance monitoring.
- Every catch block **MUST** log the exception with full context.  
  Why: Ensures exceptions are traceable with operation details and correlation IDs.
- All data create/update/delete operations **MUST** be logged.  
  Why: Provides audit trails for data mutations with entity type and identifier.
- All calls to external services **MUST** be logged with request initiation and response/failure.  
  Why: Tracks external dependencies with timing and status information.
- Grain activation and deactivation **MUST** be logged.  
  Why: Provides Orleans grain lifecycle visibility for debugging and monitoring.
- All public grain method calls **MUST** be logged with timing.  
  Why: Tracks grain operation performance and inter-grain communication patterns.
- Business rule violations **MUST** be logged with rule name and context.  
  Why: Provides business analysis detail for operations prevented by rules.
- All event append and read operations **MUST** be logged.  
  Why: Tracks event sourcing operations with stream ID, event count, and position.
- Operations taking longer than 1 second **MUST** be logged with performance metrics.  
  Why: Identifies performance bottlenecks and slow operations.
- Significant resource allocations (> 10MB memory, database connections) **MUST** be logged.  
  Why: Tracks resource usage for capacity planning and leak detection.
- Batch processing operations **MUST** be logged with item counts.  
  Why: Provides visibility into bulk operation progress and completion.
- Log messages **MUST** be descriptive enough for AI agents to understand operations and diagnose issues.  
  Why: Enables automated debugging and issue analysis.
- Structured logging **MUST** be used to log objects and properties, not just strings.  
  Why: Improves log searchability and analysis capabilities.
- Every log entry **SHOULD** include correlation IDs to trace requests across services.  
  Why: Enables distributed tracing in microservices architectures.
- Log levels **SHOULD** be used appropriately for the information being recorded.  
  Why: Maintains consistent signal-to-noise ratio in logs.
- Method parameters providing business context **SHOULD** be included in logs (avoid PII).  
  Why: Aids troubleshooting while respecting privacy requirements.
- Log execution time **SHOULD** be tracked for public API methods.  
  Why: Provides performance monitoring data for optimization.
- Orleans RequestContext **SHOULD** be used to store and propagate correlation IDs across grain calls.  
  Why: Maintains request context in distributed Orleans applications.
- Developers **SHOULD** add logging for methods with multiple decision points.  
  Why: Aids debugging of complex conditional logic.
- Developers **SHOULD** add logging for critical business processes.  
  Why: Ensures visibility into high-value operations.
- Simple getters/setters, constructors, private helpers, property accessors, and pure transformation methods **SHOULD NOT** be logged.  
  Why: Reduces noise and focuses logging on meaningful operations.
- Sensitive data (passwords, tokens, PII) **MUST NOT** be logged.  
  Why: Protects user privacy and security.
- If direct ILogger.Log calls are found during code review, agents **MUST** create scratchpad tasks to convert to LoggerExtensions pattern.  
  Why: Maintains compliance with mandatory high-performance logging pattern.

## Scope and Audience

**Audience:** All developers writing C# services or components that include logging in the Mississippi Framework.

**In scope:** LoggerExtensions pattern, high-performance logging with LoggerMessage, mandatory logging scenarios, log levels, structured logging, correlation IDs, Orleans-specific logging, sensitive data handling.

**Out of scope:** Specific logging infrastructure configuration, log aggregation tools, monitoring dashboards.

## Purpose

This document establishes mandatory logging standards ensuring consistent, observable, high-performance logging across all Mississippi Framework applications through the LoggerExtensions pattern.

## Core Principles

- Use LoggerExtensions pattern for all logging (no direct ILogger calls)
- Implement LoggerMessage with source generators
- Log structured data with meaningful properties
- Include correlation IDs for distributed tracing
- Use appropriate log levels consistently
- Write AI-debuggable messages with sufficient context
- Never log sensitive data
- Always log public APIs, exceptions, data mutations, external calls
- Log Orleans grain lifecycle and method calls
- Log business rule violations and event sourcing operations
- Track performance-critical operations and resource allocations

## LoggerExtensions Implementation Pattern

### Source Generator Pattern

The source generator pattern uses `[LoggerMessage]` attributes to generate high-performance logging code at compile time. This approach reduces boilerplate by ~50% compared to manual delegate patterns.

**Benefits:**
- **KISS (Keep It Simple):** No manual delegate definitions needed
- **DRY (Don't Repeat Yourself):** Method signature defines the message template once
- **Less boilerplate:** ~50% reduction in lines of code compared to manual patterns
- **High performance:** Compiles to high-performance delegates at build time
- **Better maintainability:** Changes to parameters automatically update the delegate

**Pattern:**
```csharp
using Microsoft.Extensions.Logging;

namespace Mississippi.SomeNamespace;

/// <summary>
/// High-performance logging extensions for SomeComponent.
/// </summary>
internal static partial class SomeComponentLoggerExtensions
{
    /// <summary>
    /// Logs when some event occurs.
    /// </summary>
    [LoggerMessage(1, LogLevel.Debug, "Event {Type} for {Key}")]
    public static partial void SomeEvent(this ILogger logger, string type, string key);

    /// <summary>
    /// Logs when an operation fails with an exception.
    /// </summary>
    [LoggerMessage(2, LogLevel.Error, "Operation {Operation} failed for {EntityId}")]
    public static partial void OperationFailed(this ILogger logger, string operation, string entityId, Exception exception);
}
```

**Key requirements:**
- Class must be declared as `partial`
- Methods must be declared as `partial` with no method body
- Use `[LoggerMessage(eventId, logLevel, "message template")]` attribute
- Parameters in message template must match method parameters (excluding `ILogger` and `Exception`)
- `Exception` parameter (if present) must be last and named `exception`

### Log Levels and Usage

#### Error Level

- **Application errors** that require immediate attention
- **Unhandled exceptions** and system failures
- **Business rule violations** that prevent normal operation
- **External service failures** that impact functionality

```csharp
// PREFERRED PATTERN: Source generator with LoggerExtensions class
public static partial class EventProcessingLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Error, "Failed to process event {EventId} for stream {StreamId}")]
    public static partial void EventProcessingFailed(this ILogger logger, string eventId, string streamId, Exception exception);
}

// Usage - ALWAYS call through the LoggerExtensions class
Logger.EventProcessingFailed(eventId, streamId, ex);

// FORBIDDEN: Direct ILogger calls like this are NOT ALLOWED
// Logger.LogError(ex, "Failed to process event {EventId} for stream {StreamId}", eventId, streamId);
```

#### Warning Level

- **Recoverable errors** that don't stop execution
- **Performance degradation** indicators
- **Deprecated feature usage**
- **Configuration issues** that have fallbacks

```csharp
// PREFERRED PATTERN: Source generator with LoggerExtensions class
public static partial class SystemMonitoringLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Warning, "High memory usage detected: {MemoryUsage}MB")]
    public static partial void HighMemoryUsageDetected(this ILogger logger, int memoryUsage);
}

// Usage - ALWAYS through LoggerExtensions
Logger.HighMemoryUsageDetected(memoryUsage);
```

#### Information Level

- **Business events** and important state changes
- **Service lifecycle events** (startup, shutdown, health checks)
- **Performance metrics** and operational data
- **User actions** that are significant

```csharp
// PREFERRED PATTERN: Source generator with LoggerExtensions
public static partial class EventSourcingLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Event {EventId} appended to stream {StreamId} at position {Position}")]
    public static partial void EventAppended(this ILogger logger, string eventId, string streamId, long position);
}

// Usage
Logger.EventAppended(eventId, streamId, position);
```

#### Debug Level

- **Detailed execution flow** for troubleshooting
- **Method entry/exit** with parameters
- **Intermediate calculation results**
- **Configuration values** and settings

```csharp
// PREFERRED PATTERN: Source generator
public static partial class EventProcessingLoggerExtensions
{
    [LoggerMessage(2, LogLevel.Debug, "Processing batch of {Count} events for stream {StreamId}")]
    public static partial void ProcessingBatch(this ILogger logger, int eventCount, string streamId);
}

// Usage
Logger.ProcessingBatch(eventCount, streamId);
```

#### Trace Level

- **Very detailed execution flow** for deep debugging
- **Loop iterations** and repetitive operations
- **Memory allocation** and garbage collection events
- **Network call details** and timing

```csharp
// PREFERRED PATTERN: Source generator
public static partial class MemoryAllocationLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Trace, "Allocated {Bytes} bytes for event buffer")]
    public static partial void BufferAllocated(this ILogger logger, int bufferSize);
}

// Usage
Logger.BufferAllocated(bufferSize);
```

## Log Message Formatting

### Event Numbers and Identifiers

- **Use consistent event numbers** - Prefix with component identifier
- **Include correlation IDs** - Link related log entries across services
- **Add request/operation IDs** - Track specific operations end-to-end

```csharp
// High-performance logging with LoggerMessage source generator
public static partial class EventProcessingLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Information, "EVT-001: Event {EventId} processed successfully")]
    public static partial void EventProcessed(this ILogger logger, string eventId);

    [LoggerMessage(2, LogLevel.Information, "EVT-001: Event {EventId} processed successfully. CorrelationId: {CorrelationId}")]
    public static partial void EventProcessedWithCorrelation(this ILogger logger, string eventId, string correlationId);
}

// Usage
Logger.EventProcessed(eventId);
Logger.EventProcessedWithCorrelation(eventId, correlationId);
```

### Structured Data Logging

- **Log objects as structured data** - Not as serialized strings
- **Use meaningful property names** - Clear, descriptive names
- **Include relevant context** - IDs, timestamps, user info, etc.
- **Write descriptive messages for AI debugging** - Include enough context for AI agents to understand the operation and diagnose issues

```csharp
// High-performance logging with LoggerMessage source generator
public static partial class OrderProcessingLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Information, "User {UserId} created order {OrderId} with {ItemCount} items")]
    public static partial void OrderCreated(this ILogger logger, string userId, string orderId, int itemCount);

    [LoggerMessage(2, LogLevel.Information, "User {UserId} created order {OrderId} with {ItemCount} items. Total: {TotalAmount:C}, Currency: {Currency}")]
    public static partial void OrderCreatedWithTotal(this ILogger logger, string userId, string orderId, int itemCount, decimal totalAmount, string currency);

    [LoggerMessage(3, LogLevel.Information, "E-commerce order creation completed successfully. User: {UserId}, Order: {OrderId}, Items: {ItemCount}, Total: {TotalAmount:C}, Currency: {Currency}, PaymentMethod: {PaymentMethod}, ShippingAddress: {ShippingCountry}")]
    public static partial void OrderCreatedComplete(this ILogger logger, string userId, string orderId, int itemCount, decimal totalAmount, string currency, string paymentMethod, string shippingCountry);
}

// Usage
Logger.OrderCreated(userId, orderId, itemCount);
Logger.OrderCreatedWithTotal(userId, orderId, itemCount, totalAmount, currency);
Logger.OrderCreatedComplete(userId, orderId, itemCount, totalAmount, currency, paymentMethod, shippingCountry);
```

### Exception Logging

- **Always include the exception object** - Don't just log the message
- **Add context information** - What operation was being performed
- **Include correlation data** - Request ID, user ID, etc.
- **Provide AI-debuggable context** - Include enough information for AI agents to understand what failed and why

```csharp
// High-performance logging with LoggerMessage source generator
public static partial class EventProcessingLoggerExtensions
{
    [LoggerMessage(3, LogLevel.Error, "Failed to process event {EventId} for stream {StreamId}. CorrelationId: {CorrelationId}")]
    public static partial void EventProcessingFailed(this ILogger logger, string eventId, string streamId, string correlationId, Exception exception);

    [LoggerMessage(4, LogLevel.Error, "Event processing failed during stream operation. EventId: {EventId}, StreamId: {StreamId}, EventType: {EventType}, EventVersion: {EventVersion}, CorrelationId: {CorrelationId}, ProcessingStep: {ProcessingStep}, RetryCount: {RetryCount}")]
    public static partial void EventProcessingFailedDetailed(this ILogger logger, string eventId, string streamId, string eventType, string eventVersion, string correlationId, string processingStep, int retryCount, Exception exception);
}

// Usage
try
{
    await ProcessEventAsync(eventData);
}
catch (Exception ex)
{
    Logger.EventProcessingFailed(eventId, streamId, correlationId, ex);
    throw;
}

// Better - AI-debuggable exception logging
try
{
    await ProcessEventAsync(eventData);
}
catch (Exception ex)
{
    Logger.EventProcessingFailedDetailed(eventData.Id, streamId, eventData.Type, eventData.Version, correlationId, "EventValidation", retryCount, ex);
    throw;
}
```

## Performance and Sensitive Data

### High-Performance Logging with LoggerMessage

- **MANDATORY: Always use LoggerMessage source generators for ALL logging** - Direct ILogger calls are FORBIDDEN
- **MANDATORY: Use LoggerExtensions classes** - ALL logging must be implemented in `public static partial class [ComponentName]LoggerExtensions` classes
- **Use strongly-typed parameters** - Avoid boxing of value types
- **NO EXCEPTIONS** - This pattern is required for all logging, without exception

```csharp
// MANDATORY PATTERN: LoggerExtensions class with LoggerMessage source generator
public static partial class EventProcessorLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Event {EventId} processed for stream {StreamId}")]
    public static partial void EventProcessed(this ILogger logger, string eventId, string streamId);

    [LoggerMessage(2, LogLevel.Error, "Failed to process event {EventId} for stream {StreamId}")]
    public static partial void EventProcessingFailed(this ILogger logger, string eventId, string streamId, Exception exception);
}

// Usage in Orleans grain
public sealed class EventProcessorGrain : IGrainBase, IEventProcessorGrain
{
    public IGrainContext GrainContext { get; }
    private ILogger<EventProcessorGrain> Logger { get; }

    public EventProcessorGrain(IGrainContext grainContext, ILogger<EventProcessorGrain> logger)
    {
        GrainContext = grainContext;
        Logger = logger;
    }

    public async Task ProcessEventAsync(string eventId, string streamId)
    {
        try
        {
            await ProcessEventInternalAsync(eventId, streamId);
            Logger.EventProcessed(eventId, streamId);
        }
        catch (Exception ex)
        {
            Logger.EventProcessingFailed(eventId, streamId, ex);
            throw;
        }
    }
}
```

### Performance Considerations

- **Use log level checks** - Avoid expensive operations for disabled log levels
- **Lazy evaluation** - Use lambda expressions for expensive operations
- **Async logging** - Use async logging providers when available

```csharp
// MANDATORY PATTERN: LoggerExtensions class with LoggerMessage source generator
public static partial class PerformanceServiceLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "Expensive operation result: {Result}")]
    public static partial void ExpensiveOperationResult(this ILogger logger, string result);

    [LoggerMessage(2, LogLevel.Debug, "User data: {@UserData}")]
    public static partial void UserDataLogged(this ILogger logger, object userData);
}

// Performance-optimized logging - ALWAYS through LoggerExtensions
if (Logger.IsEnabled(LogLevel.Debug))
{
    var result = await ExpensiveOperationAsync();
    Logger.ExpensiveOperationResult(result);
}

// Lazy evaluation - ALWAYS through LoggerExtensions
Logger.UserDataLogged(userData); // Only serializes if Debug is enabled
```

### Sensitive Data Handling

- **Never log sensitive information** - Passwords, tokens, PII, etc.
- **Mask or redact sensitive data** - Use placeholders for sensitive fields
- **Log data classifications** - Indicate when data has been redacted

```csharp
// High-performance logging with LoggerMessage source generator
public static partial class SecurityLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Information, "User {UserId} authenticated. Token: [REDACTED]")]
    public static partial void UserAuthenticated(this ILogger logger, string userId);

    [LoggerMessage(2, LogLevel.Information, "Processing user data. PII fields redacted for privacy compliance")]
    public static partial void PiiDataRedacted(this ILogger logger);
}

// Usage
Logger.UserAuthenticated(userId);
Logger.PiiDataRedacted();
```

## Context and Correlation

### AI-Debuggable Logging

- **Write descriptive log messages** - Include enough context for AI agents to understand operations
- **Log the "why" not just the "what"** - Explain the business context and decision points
- **Include state information** - Log relevant system state, configuration, and environment details
- **Use consistent terminology** - Use clear, domain-specific language that AI agents can understand
- **Log decision points** - Include information about business rules, thresholds, and decision logic

### Correlation IDs

- **Include correlation IDs** in all log entries
- **Propagate correlation IDs** across service boundaries
- **Use consistent correlation ID format** - UUID or similar unique identifier

```csharp
// High-performance logging with LoggerMessage source generator
public static partial class OperationLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Starting operation. CorrelationId: {CorrelationId}")]
    public static partial void OperationStarted(this ILogger logger, string correlationId);

    [LoggerMessage(2, LogLevel.Information, "Starting user authentication workflow. Operation: {Operation}, UserId: {UserId}, AuthMethod: {AuthMethod}, CorrelationId: {CorrelationId}, Environment: {Environment}")]
    public static partial void AuthenticationWorkflowStarted(this ILogger logger, string operation, string userId, string authMethod, string correlationId, string environment);
}

// Usage
var correlationId = GetCorrelationId(context) ?? Guid.NewGuid().ToString();

Logger.OperationStarted(correlationId);
Logger.AuthenticationWorkflowStarted("UserLogin", userId, authMethod, correlationId, environment);
```

### Request Context

- **Log request details** - Method, path, user agent, etc.
- **Include timing information** - Request duration, processing time
- **Add business context** - User ID, tenant ID, feature flags

```csharp
// High-performance logging with LoggerMessage source generator
public static partial class HttpRequestLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Information, "HTTP {Method} {Path} completed in {Duration}ms. User: {UserId}, Tenant: {TenantId}")]
    public static partial void HttpRequestCompleted(this ILogger logger, string method, string path, long duration, string userId, string tenantId);

    [LoggerMessage(2, LogLevel.Information, "HTTP request completed successfully. Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, Duration: {Duration}ms, User: {UserId}, Tenant: {TenantId}, UserAgent: {UserAgent}, IPAddress: {IPAddress}, RequestSize: {RequestSize}bytes, ResponseSize: {ResponseSize}bytes")]
    public static partial void HttpRequestCompletedDetailed(this ILogger logger, string method, string path, int statusCode, long duration, string userId, string tenantId, string userAgent, string ipAddress, long requestSize, long responseSize);
}

// Usage
Logger.HttpRequestCompleted(method, path, duration, userId, tenantId);
Logger.HttpRequestCompletedDetailed(method, path, statusCode, duration, userId, tenantId, userAgent, ipAddress, requestSize, responseSize);
```

## Mississippi Framework Specific Logging

### Event Sourcing Operations

- **Log event append operations** - Include stream ID, event count, position
- **Record read operations** - Stream ID, position range, result count
- **Track grain operations** - Grain ID, operation type, duration

```csharp
// High-performance logging with LoggerMessage source generator
public static partial class EventSourcingOperationsLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Information, "EVT-APPEND: Appended {EventCount} events to stream {StreamId} at position {Position}. CorrelationId: {CorrelationId}")]
    public static partial void EventsAppended(this ILogger logger, int eventCount, string streamId, long position, string correlationId);

    [LoggerMessage(2, LogLevel.Information, "EVT-READ: Read {EventCount} events from stream {StreamId} positions {StartPosition}-{EndPosition}. CorrelationId: {CorrelationId}")]
    public static partial void EventsRead(this ILogger logger, int eventCount, string streamId, long startPosition, long endPosition, string correlationId);
}

// Usage
Logger.EventsAppended(eventCount, streamId, position, correlationId);
Logger.EventsRead(eventCount, streamId, startPosition, endPosition, correlationId);
```

### Orleans Grain Operations

- **Log grain activation/deactivation** - Include grain ID and type
- **Record grain method calls** - Method name, parameters, duration
- **Track grain state changes** - State transitions and reasons
- **Use Orleans-specific logging patterns** - Leverage Orleans telemetry and grain context
- **Handle correlation ID propagation** - Pass correlation IDs between grains and maintain request context

```csharp
// High-performance logging with LoggerMessage source generator
public static partial class EventSourcingGrainLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Information, "GRAIN-ACTIVATE: Grain {GrainType} activated with ID {GrainId}. PrimaryKey: {PrimaryKey}")]
    public static partial void GrainActivated(this ILogger logger, string grainType, string grainId, string primaryKey);

    [LoggerMessage(2, LogLevel.Information, "GRAIN-DEACTIVATE: Grain {GrainType} deactivated with ID {GrainId}. Reason: {DeactivationReason}")]
    public static partial void GrainDeactivated(this ILogger logger, string grainType, string grainId, string deactivationReason);

    [LoggerMessage(3, LogLevel.Information, "GRAIN-METHOD-START: {MethodName} called on grain {GrainId}. EventId: {EventId}, CorrelationId: {CorrelationId}")]
    public static partial void GrainMethodStarted(this ILogger logger, string methodName, string grainId, string eventId, string correlationId);

    [LoggerMessage(4, LogLevel.Information, "GRAIN-METHOD-SUCCESS: {MethodName} completed on grain {GrainId}. Duration: {Duration}ms, EventId: {EventId}")]
    public static partial void GrainMethodSuccess(this ILogger logger, string methodName, string grainId, long duration, string eventId);

    [LoggerMessage(5, LogLevel.Error, "GRAIN-METHOD-ERROR: {MethodName} failed on grain {GrainId}. Duration: {Duration}ms, EventId: {EventId}, CorrelationId: {CorrelationId}")]
    public static partial void GrainMethodError(this ILogger logger, string methodName, string grainId, long duration, string eventId, string correlationId, Exception exception);
}

// Orleans grain with proper logging patterns and correlation ID handling
public sealed class EventSourcingGrain : IEventSourcingGrain, IGrainBase
{
    public IGrainContext GrainContext { get; }
    private ILogger<EventSourcingGrain> Logger { get; }

    public EventSourcingGrain(IGrainContext grainContext, ILogger<EventSourcingGrain> logger)
    {
        GrainContext = grainContext;
        Logger = logger;
    }

    public async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        EventSourcingGrainLoggerExtensions.GrainActivated(Logger, this.GetType().Name, this.GetPrimaryKeyString(), this.GetPrimaryKeyString());
        await Task.CompletedTask;
    }

    public async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        EventSourcingGrainLoggerExtensions.GrainDeactivated(Logger, this.GetType().Name, this.GetPrimaryKeyString(), reason.Description);
        await Task.CompletedTask;
    }

    public async Task<Result> ProcessEventAsync(EventData eventData)
    {
        var grainId = this.GetPrimaryKeyString();
        var methodName = nameof(ProcessEventAsync);
        var correlationId = eventData.CorrelationId ?? GetOrCreateCorrelationId();
        
        EventSourcingGrainLoggerExtensions.GrainMethodStarted(Logger, methodName, grainId, eventData.Id, correlationId);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await ProcessEventInternalAsync(eventData, correlationId);
            stopwatch.Stop();
            
            EventSourcingGrainLoggerExtensions.GrainMethodSuccess(Logger, methodName, grainId, stopwatch.ElapsedMilliseconds, eventData.Id);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            EventSourcingGrainLoggerExtensions.GrainMethodError(Logger, methodName, grainId, stopwatch.ElapsedMilliseconds, eventData.Id, correlationId, ex);
            throw;
        }
    }

    private async Task<Result> ProcessEventInternalAsync(EventData eventData, string correlationId)
    {
        // Example of calling another grain with correlation ID propagation
        var validationGrain = GrainFactory.GetGrain<IValidationGrain>(eventData.ValidationKey);
        
        // Pass correlation ID to the next grain
        var validationResult = await validationGrain.ValidateAsync(eventData, correlationId);
        
        if (!validationResult.IsValid)
        {
            EventSourcingGrainLoggerExtensions.ValidationFailed(Logger, eventData.Id, correlationId, validationResult.Errors);
            return Result.Failure(validationResult.Errors);
        }

        // Continue processing with correlation ID
        return await ContinueProcessingAsync(eventData, correlationId);
    }

    private string GetOrCreateCorrelationId()
    {
        // Try to get correlation ID from Orleans RequestContext
        var correlationId = RequestContext.Get("CorrelationId") as string;
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            RequestContext.Set("CorrelationId", correlationId);
        }
        return correlationId;
    }
}
```

### Orleans Correlation ID and Request Context Handling

- **Use Orleans RequestContext** - Store and retrieve correlation IDs across grain calls
- **Propagate correlation IDs** - Pass correlation IDs as parameters to other grains
- **Handle missing correlation IDs** - Create new correlation IDs when none exist
- **Log grain-to-grain communication** - Track inter-grain requests with correlation IDs
- **Use Orleans telemetry** - Leverage built-in Orleans observability features

```csharp
// High-performance logging with LoggerMessage source generator for Orleans correlation handling
public static partial class OrleansCorrelationLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "Correlation ID {CorrelationId} propagated from grain {SourceGrain} to {TargetGrain}")]
    public static partial void CorrelationIdPropagated(this ILogger logger, string correlationId, string sourceGrain, string targetGrain);

    [LoggerMessage(2, LogLevel.Debug, "New correlation ID {CorrelationId} created for grain {GrainType}")]
    public static partial void CorrelationIdCreated(this ILogger logger, string correlationId, string grainType);

    [LoggerMessage(3, LogLevel.Debug, "Grain call started. Method: {MethodName}, Target: {TargetGrain}, CorrelationId: {CorrelationId}")]
    public static partial void GrainCallStarted(this ILogger logger, string methodName, string targetGrain, string correlationId);

    [LoggerMessage(4, LogLevel.Debug, "Grain call completed. Method: {MethodName}, Target: {TargetGrain}, CorrelationId: {CorrelationId}, Duration: {Duration}ms")]
    public static partial void GrainCallCompleted(this ILogger logger, string methodName, string targetGrain, string correlationId, long duration);

    [LoggerMessage(5, LogLevel.Error, "Grain call failed. Method: {MethodName}, Target: {TargetGrain}, CorrelationId: {CorrelationId}, Duration: {Duration}ms, Error: {Error}")]
    public static partial void GrainCallFailed(this ILogger logger, string methodName, string targetGrain, string correlationId, long duration, string error, Exception exception);
}

// Orleans correlation ID helper class
public static class OrleansCorrelationHelper
{
    public static string GetOrCreateCorrelationId()
    {
        var correlationId = RequestContext.Get("CorrelationId") as string;
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            RequestContext.Set("CorrelationId", correlationId);
        }
        return correlationId;
    }

    public static void SetCorrelationId(string correlationId)
    {
        RequestContext.Set("CorrelationId", correlationId);
    }

    public static string GetCorrelationId()
    {
        return RequestContext.Get("CorrelationId") as string ?? string.Empty;
    }

    public static async Task<T> CallGrainWithCorrelationAsync<T>(IGrain grain, Func<Task<T>> grainCall, string methodName, ILogger logger)
    {
        var correlationId = GetOrCreateCorrelationId();
        var targetGrain = grain.GetType().Name;
        
        logger.GrainCallStarted(methodName, targetGrain, correlationId);
        
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await grainCall();
            stopwatch.Stop();
            
            logger.GrainCallCompleted(methodName, targetGrain, correlationId, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.GrainCallFailed(methodName, targetGrain, correlationId, stopwatch.ElapsedMilliseconds, ex.Message, ex);
            throw;
        }
    }
}

// Example of calling another grain with correlation ID propagation (POCO grain pattern)
public sealed class OrderProcessingGrain : IOrderProcessingGrain, IGrainBase
{
    public IGrainContext GrainContext { get; }
    private ILogger<OrderProcessingGrain> Logger { get; }

    public OrderProcessingGrain(IGrainContext grainContext, ILogger<OrderProcessingGrain> logger)
    {
        GrainContext = grainContext;
        Logger = logger;
    }

    public async Task<OrderResult> ProcessOrderAsync(OrderRequest request)
    {
        var correlationId = OrleansCorrelationHelper.GetOrCreateCorrelationId();
        OrderProcessingGrainLoggerExtensions.ProcessingOrder(Logger, request.OrderId, correlationId);

        // Call inventory grain with correlation ID propagation
        var inventoryGrain = GrainFactory.GetGrain<IInventoryGrain>(request.ProductId);
        var inventoryResult = await OrleansCorrelationHelper.CallGrainWithCorrelationAsync(
            inventoryGrain,
            () => inventoryGrain.CheckAvailabilityAsync(request.ProductId, request.Quantity),
            nameof(IInventoryGrain.CheckAvailabilityAsync),
            Logger);

        if (!inventoryResult.IsAvailable)
        {
            OrderProcessingGrainLoggerExtensions.ProductUnavailable(Logger, request.ProductId, request.OrderId, correlationId);
            return OrderResult.OutOfStock();
        }

        // Call payment grain with correlation ID propagation
        var paymentGrain = GrainFactory.GetGrain<IPaymentGrain>(request.CustomerId);
        var paymentResult = await OrleansCorrelationHelper.CallGrainWithCorrelationAsync(
            paymentGrain,
            () => paymentGrain.ProcessPaymentAsync(request.PaymentInfo, request.TotalAmount),
            nameof(IPaymentGrain.ProcessPaymentAsync),
            Logger);

        if (!paymentResult.IsSuccessful)
        {
            OrderProcessingGrainLoggerExtensions.PaymentFailed(Logger, request.OrderId, paymentResult.ErrorMessage, correlationId);
            return OrderResult.PaymentFailed(paymentResult.ErrorMessage);
        }

        OrderProcessingGrainLoggerExtensions.OrderProcessed(Logger, request.OrderId, correlationId);
        return OrderResult.Success();
    }
}
```

### Cosmos DB Operations

- **Log database operations** - Read/write operations, batch sizes
- **Record performance metrics** - Request units, latency, throughput
- **Track retry attempts** - Retry count, backoff strategy, success/failure

```csharp
// High-performance logging with LoggerMessage source generator
public static partial class CosmosDbLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Information, "COSMOS-OP: {Operation} on container {Container}. RequestUnits: {RequestUnits}, Duration: {Duration}ms. CorrelationId: {CorrelationId}")]
    public static partial void CosmosOperation(this ILogger logger, string operation, string container, double requestUnits, long duration, string correlationId);

    [LoggerMessage(2, LogLevel.Warning, "COSMOS-RETRY: Retry {RetryCount} for operation {Operation}. Backoff: {BackoffMs}ms. CorrelationId: {CorrelationId}")]
    public static partial void CosmosRetry(this ILogger logger, int retryCount, string operation, long backoffMs, string correlationId);
}

// Usage
Logger.CosmosOperation(operation, container, requestUnits, duration, correlationId);
Logger.CosmosRetry(retryCount, operation, backoffMs, correlationId);
```

## Logging Configuration

### Application Settings

- **Configure log levels** appropriately for each environment
- **Set up structured logging** with JSON formatting
- **Configure correlation ID extraction** from headers or context
- **Enable high-performance logging** with LoggerMessage support

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.Orleans": "Information",
      "Mississippi": "Debug"
    },
    "Console": {
      "FormatterName": "json",
      "FormatterOptions": {
        "IncludeScopes": true,
        "TimestampFormat": "yyyy-MM-dd HH:mm:ss.fff ",
        "UseUtcTimestamp": true
      }
    },
    "EventSource": {
      "LogLevel": {
        "Default": "Warning"
      }
    }
  }
}
```

### Correlation ID Setup

- **Extract from HTTP headers** - X-Correlation-ID, X-Request-ID
- **Create for background operations** - Use GUID or similar
- **Propagate across async boundaries** - Use AsyncLocal or similar
- **Bridge ASP.NET to Orleans** - Pass correlation IDs from HTTP context to grain calls

```csharp
// High-performance logging with LoggerMessage source generator
public static partial class CorrelationIdMiddlewareLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Request started. CorrelationId: {CorrelationId}")]
    public static partial void RequestStarted(this ILogger logger, string correlationId);
}

// Correlation ID middleware with Orleans integration
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private ILogger<CorrelationIdMiddleware> Logger { get; }

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        Logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
            ?? Guid.NewGuid().ToString();
        
        context.Items["CorrelationId"] = correlationId;
        Logger.RequestStarted(correlationId);
        
        // Set correlation ID in Orleans RequestContext for grain calls
        OrleansCorrelationHelper.SetCorrelationId(correlationId);
        
        await _next(context);
    }
}

// Controller example showing how to call grains with correlation ID
[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController : ControllerBase
{
    private readonly IClusterClient _clusterClient;
    private ILogger<OrdersController> Logger { get; }

    public OrdersController(IClusterClient clusterClient, ILogger<OrdersController> logger)
    {
        _clusterClient = clusterClient;
        Logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        // Correlation ID is already set in Orleans RequestContext by middleware
        var correlationId = OrleansCorrelationHelper.GetCorrelationId();
        
        OrdersControllerLoggerExtensions.CreatingOrder(Logger, request.CustomerId, correlationId);

        try
        {
            var orderGrain = _clusterClient.GetGrain<IOrderProcessingGrain>(request.CustomerId);
            
            // The correlation ID will automatically propagate to the grain
            var result = await orderGrain.ProcessOrderAsync(request);
            
            OrdersControllerLoggerExtensions.OrderCreationCompleted(Logger, request.CustomerId, result.Status, correlationId);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            OrdersControllerLoggerExtensions.OrderCreationFailed(Logger, request.CustomerId, correlationId, ex);
            return StatusCode(500, "Order creation failed");
        }
    }
}

// Alternative approach: Pass correlation ID explicitly to grains
public sealed class ExplicitCorrelationController : ControllerBase
{
    private readonly IClusterClient _clusterClient;
    private ILogger<ExplicitCorrelationController> Logger { get; }

    public ExplicitCorrelationController(IClusterClient clusterClient, ILogger<ExplicitCorrelationController> logger)
    {
        _clusterClient = clusterClient;
        Logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var correlationId = HttpContext.Items["CorrelationId"] as string ?? Guid.NewGuid().ToString();
        
        OrdersControllerLoggerExtensions.CreatingOrder(Logger, request.CustomerId, correlationId);

        try
        {
            var orderGrain = _clusterClient.GetGrain<IOrderProcessingGrain>(request.CustomerId);
            
            // Pass correlation ID explicitly in the request
            var requestWithCorrelation = new OrderRequestWithCorrelation
            {
                OrderData = request,
                CorrelationId = correlationId
            };
            
            var result = await orderGrain.ProcessOrderWithCorrelationAsync(requestWithCorrelation);
            
            OrdersControllerLoggerExtensions.OrderCreationCompleted(Logger, request.CustomerId, result.Status, correlationId);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            OrdersControllerLoggerExtensions.OrderCreationFailed(Logger, request.CustomerId, correlationId, ex);
            return StatusCode(500, "Order creation failed");
        }
    }
}
```

## Monitoring and Alerting

### Key Metrics to Monitor

- **Error rates** - Percentage of error logs vs total logs
- **Performance indicators** - Log entries with high duration values
- **Business events** - Important business operations and their success rates
- **System health** - Startup, shutdown, and health check events

### Alerting Rules

- **High error rates** - Alert when error percentage exceeds threshold
- **Performance degradation** - Alert on slow operations
- **Missing correlation IDs** - Alert when logs lack correlation data
- **Sensitive data exposure** - Alert on potential PII logging

## Compliance and Audit

### Audit Requirements

- **Log all security events** - Authentication, authorization, data access
- **Record data modifications** - Create, update, delete operations
- **Track configuration changes** - Settings modifications and deployments
- **Maintain retention policies** - Keep logs for required time periods

### Data Protection

- **GDPR compliance** - Ensure PII is properly handled
- **Data retention** - Implement appropriate log retention policies
- **Access controls** - Restrict log access to authorized personnel
- **Encryption** - Encrypt logs at rest and in transit

## Tools and Integration

### Recommended Tools

- **Microsoft.Extensions.Logging** - Standard .NET logging framework
- **OpenTelemetry** - Vendor-neutral observability framework
- **Console/File/EventLog providers** - Built-in .NET logging providers

### Integration Patterns

- **Centralized logging** - Send all logs to a central repository
- **Log aggregation** - Collect logs from multiple services
- **Real-time monitoring** - Set up dashboards for live log monitoring
- **OpenTelemetry integration** - Use standard observability protocols for vendor-neutral logging
- **Structured logging** - Use JSON formatting for better log parsing and analysis

## Code Review Checklist

When reviewing code that includes logging, ensure:

- [ ] Uses dependency-injected `ILogger<T>` as a property
- [ ] **MANDATORY: ALL logging uses LoggerExtensions classes** - Must follow `public static partial class [ComponentName]LoggerExtensions` pattern
- [ ] **MANDATORY: NO direct ILogger calls** - All logging must go through static extension methods
- [ ] **MANDATORY: Uses LoggerMessage source generators** - All logging must use `[LoggerMessage]` attributes on partial methods
- [ ] Includes appropriate correlation IDs
- [ ] Uses correct log levels
- [ ] Logs structured data, not just strings
- [ ] Includes exception objects for error logging
- [ ] Avoids logging sensitive information
- [ ] Includes relevant context and metadata
- [ ] Follows naming conventions for event numbers
- [ ] Provides sufficient detail for troubleshooting
- [ ] Uses Orleans-specific patterns for grain logging
- [ ] Implements proper grain lifecycle logging (activation/deactivation)
- [ ] Writes AI-debuggable log messages with sufficient context
- [ ] Includes business context and decision points in log messages
- [ ] Uses clear, descriptive language that AI agents can understand
- [ ] If direct `ILogger.Log*` calls are found, open a `.scratchpad/tasks` item to convert to LoggerExtensions with LoggerMessage (see Agent Scratchpad)

## Examples and Templates

### MANDATORY Scenario Examples

#### Public Service Method with Exception Handling

```csharp
public static partial class OrderServiceLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Processing order {OrderId} for customer {CustomerId}")]
    public static partial void ProcessOrderStarted(this ILogger logger, string orderId, string customerId);

    [LoggerMessage(2, LogLevel.Information, "Order {OrderId} processed successfully with status {Status}")]
    public static partial void ProcessOrderCompleted(this ILogger logger, string orderId, string status);

    [LoggerMessage(3, LogLevel.Error, "Failed to process order {OrderId}")]
    public static partial void ProcessOrderFailed(this ILogger logger, string orderId, Exception exception);
}

// MANDATORY: Public service method with logging
public class OrderService
{
    private ILogger<OrderService> Logger { get; }

    public OrderService(ILogger<OrderService> logger)
    {
        Logger = logger;
    }

    public async Task<OrderResult> ProcessOrderAsync(OrderRequest request)
    {
        Logger.ProcessOrderStarted(request.OrderId, request.CustomerId);
        try
        {
            var result = await ProcessOrderInternalAsync(request);
            Logger.ProcessOrderCompleted(request.OrderId, result.Status);
            return result;
        }
        catch (Exception ex)
        {
            Logger.ProcessOrderFailed(request.OrderId, ex);
            throw;
        }
    }
}
```

#### Data Mutation Operations

```csharp
public static partial class UserRepositoryLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Creating user {UserId} with email {Email}")]
    public static partial void UserCreationStarted(this ILogger logger, string userId, string email);

    [LoggerMessage(2, LogLevel.Information, "User {UserId} created successfully")]
    public static partial void UserCreated(this ILogger logger, string userId);

    [LoggerMessage(3, LogLevel.Error, "Failed to create user {UserId}")]
    public static partial void UserCreationFailed(this ILogger logger, string userId, Exception exception);
}

// MANDATORY: Data mutation logging
public class UserRepository
{
    private ILogger<UserRepository> Logger { get; }

    public UserRepository(ILogger<UserRepository> logger)
    {
        Logger = logger;
    }

    public async Task CreateUserAsync(User user)
    {
        Logger.UserCreationStarted(user.Id, user.Email);
        try
        {
            await SaveUserToDatabase(user);
            Logger.UserCreated(user.Id);
        }
        catch (Exception ex)
        {
            Logger.UserCreationFailed(user.Id, ex);
            throw;
        }
    }
}
```

#### Orleans Grain with Mandatory Logging

```csharp
public static partial class EventProcessingGrainLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Information, "Grain {GrainType} activated with ID {GrainId}")]
    public static partial void GrainActivated(this ILogger logger, string grainType, string grainId);

    [LoggerMessage(2, LogLevel.Information, "Method {MethodName} started on grain {GrainId} for event {EventId}")]
    public static partial void GrainMethodStarted(this ILogger logger, string methodName, string grainId, string eventId);

    [LoggerMessage(3, LogLevel.Information, "Method {MethodName} completed on grain {GrainId} for event {EventId} in {Duration}ms")]
    public static partial void GrainMethodCompleted(this ILogger logger, string methodName, string grainId, string eventId, long duration);
}

// MANDATORY: Orleans grain logging
public class EventProcessingGrain : IEventProcessingGrain, IGrainBase
{
    private ILogger<EventProcessingGrain> Logger { get; }

    public EventProcessingGrain(ILogger<EventProcessingGrain> logger)
    {
        Logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        EventProcessingGrainLoggerExtensions.GrainActivated(Logger, this.GetType().Name, this.GetPrimaryKeyString());
        await base.OnActivateAsync(cancellationToken);
    }

    public async Task<EventResult> ProcessEventAsync(EventData eventData)
    {
        var stopwatch = Stopwatch.StartNew();
        EventProcessingGrainLoggerExtensions.GrainMethodStarted(Logger, nameof(ProcessEventAsync), this.GetPrimaryKeyString(), eventData.Id);
        
        try
        {
            var result = await ProcessEventInternalAsync(eventData);
            stopwatch.Stop();
            EventProcessingGrainLoggerExtensions.GrainMethodCompleted(Logger, nameof(ProcessEventAsync), this.GetPrimaryKeyString(), eventData.Id, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            EventProcessingGrainLoggerExtensions.GrainMethodFailed(Logger, nameof(ProcessEventAsync), this.GetPrimaryKeyString(), eventData.Id, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
    }
}
```

### Standard Logging Template

```csharp
// PREFERRED PATTERN: Source generator with LoggerExtensions class
public static partial class ExampleServiceLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Information, "PROC-001: Starting data processing. DataLength: {DataLength}, CorrelationId: {CorrelationId}")]
    public static partial void DataProcessingStarted(this ILogger logger, int dataLength, string correlationId);

    [LoggerMessage(2, LogLevel.Information, "PROC-002: Data processing completed successfully. Result: {Result}, CorrelationId: {CorrelationId}")]
    public static partial void DataProcessingCompleted(this ILogger logger, string result, string correlationId);

    [LoggerMessage(3, LogLevel.Error, "PROC-ERROR: Data processing failed. DataLength: {DataLength}, CorrelationId: {CorrelationId}")]
    public static partial void DataProcessingFailed(this ILogger logger, int dataLength, string correlationId, Exception exception);
}

public class ExampleService
{
    private ILogger<ExampleService> Logger { get; }

    public ExampleService(ILogger<ExampleService> logger)
    {
        Logger = logger;
    }

    public async Task<Result> ProcessDataAsync(string data, string correlationId)
    {
        Logger.DataProcessingStarted(data.Length, correlationId);

        try
        {
            var result = await ProcessAsync(data);
            
            Logger.DataProcessingCompleted(result, correlationId);
            return result;
        }
        catch (Exception ex)
        {
            Logger.DataProcessingFailed(data.Length, correlationId, ex);
            throw;
        }
    }
}

// FORBIDDEN PATTERNS - DO NOT USE:
// Logger.LogInformation("Starting data processing. DataLength: {DataLength}", data.Length); // WRONG - Direct ILogger call
// Logger.LogError(ex, "Processing failed"); // WRONG - Direct ILogger call
// ANY direct ILogger.Log*() method calls are FORBIDDEN
```

This logging standard ensures consistent, observable, and maintainable logging across all Mississippi Framework applications and related services. The mandatory LoggerExtensions pattern with high-performance LoggerMessage implementation is designed to work with standard .NET tooling and provides optimal performance while maintaining consistency across the codebase.

## CRITICAL REQUIREMENTS SUMMARY

### Technical Requirements (HOW to log)

1. **MANDATORY: LoggerExtensions Classes** - ALL logging must be implemented using `public static partial class [ComponentName]LoggerExtensions` pattern
2. **MANDATORY: LoggerMessage Source Generators** - ALL logging must use `[LoggerMessage]` attributes on partial methods
3. **FORBIDDEN: Direct ILogger calls** - NO direct ILogger.Log*() method calls are allowed anywhere in the codebase
4. **MANDATORY: High-Performance Pattern** - This is not optional - it's required for all logging without exception

### Behavioral Requirements (WHEN to log)

1. **MANDATORY: Public Service Methods** - ALL public service methods must have entry/exit logging
2. **MANDATORY: Exception Handling** - ALL catch blocks must log exceptions with context
3. **MANDATORY: Data Mutations** - ALL create/update/delete operations must be logged
4. **MANDATORY: External Service Calls** - ALL external service interactions must be logged
5. **MANDATORY: Orleans Grain Operations** - ALL grain methods and lifecycle events must be logged
6. **MANDATORY: Business Rule Violations** - ALL business rule failures must be logged with context

### Agent Implementation Rule

When an AI agent encounters any of the mandatory scenarios above (#5-10), it MUST add appropriate logging following the LoggerExtensions pattern (#1-4). This ensures deterministic behavior and complete observability.

Failure to follow these patterns will result in code review rejection and build failures.

## References

- [High-performance logging in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging) - Microsoft documentation on LoggerMessage and performance optimization
- [Microsoft Orleans Documentation](https://learn.microsoft.com/en-us/dotnet/orleans/) - Orleans-specific patterns and best practices
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/) - Vendor-neutral observability framework
