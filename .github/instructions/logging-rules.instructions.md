---
applyTo: '**/*.cs'
---

# Enterprise Logging Best Practices

This document defines the logging standards and best practices for the Mississippi Framework and all related applications. All logging must follow these guidelines to ensure consistency, observability, and maintainability across the enterprise.

## Core Logging Principles

### MANDATORY High-Performance Logging Requirements

#### LoggerExtensions Classes - REQUIRED PATTERN
- **ALL logging MUST use the LoggerExtensions pattern** - Create a `public static class [ComponentName]LoggerExtensions` for each component
- **NO direct ILogger calls allowed** - All logging must go through static extension methods defined in LoggerExtensions classes
- **Consistent naming convention** - Class name MUST end with `LoggerExtensions` (e.g., `EventProcessingLoggerExtensions`, `OrderProcessingLoggerExtensions`)
- **Static Action delegates required** - All LoggerMessage patterns must be implemented as private static readonly Action delegates
- **Extension method pattern** - Each log operation must be exposed as a public static extension method

#### Performance Requirements
- **LoggerMessage is mandatory** - NEVER use direct ILogger.Log() calls, always use LoggerMessage.Define pattern
- **Static Action caching** - Cache all log message templates as static Action delegates for optimal performance
- **Zero allocations** - Use strongly-typed parameters to avoid boxing of value types
- **Compile-time optimization** - Leverage LoggerMessage for pre-compiled message templates

### Structured Logging with ILogger
- **Always use dependency-injected `ILogger<T>` as a property** - Use `private ILogger<T> Logger { get; }` pattern
- **Use structured logging** - Log objects and properties, not just strings
- **Include correlation IDs** - Every log entry should be traceable across services
- **Log at appropriate levels** - Use the correct log level for the information being recorded
- **MANDATORY: Use high-performance logging patterns** - ALWAYS use LoggerMessage with static Action delegates - this is required, not optional
- **MANDATORY: Use LoggerExtensions classes** - ALL logging MUST be implemented using `public static class [ComponentName]LoggerExtensions` pattern - this is a hard requirement
- **Write AI-debuggable messages** - Ensure log messages are descriptive enough for AI agents to understand and debug issues

## MANDATORY Logging Scenarios - When to Add Logging

This section defines **exactly when logging MUST be added** to ensure deterministic behavior for coding agents and complete observability across the Mississippi Framework.

### ALWAYS Add Logging For These Operations (REQUIRED)

#### 1. Method Entry/Exit for Public APIs (MANDATORY)
- **All public service methods** must log entry and successful completion
- **Include method parameters** that provide business context (avoid PII)
- **Log execution time** for performance monitoring

#### 2. Exception Handling (MANDATORY)
- **Every catch block MUST log the exception** with full context
- **Include operation details** that were being performed when the exception occurred
- **Add correlation IDs** to track exceptions across service boundaries

#### 3. Data Mutations (MANDATORY)
- **All data create/update/delete operations** must be logged
- **Include entity type and identifier** for audit trails
- **Log both start and completion** of data operations

#### 4. External Service Calls (MANDATORY)
- **All calls to external services** (APIs, databases, file systems)
- **Log request initiation and response/failure**
- **Include timing and status information**

#### 5. Orleans Grain Lifecycle (MANDATORY)
- **Grain activation and deactivation** must be logged
- **All public grain method calls** with timing
- **Inter-grain communication** with target grain information

#### 6. Business Rule Violations (MANDATORY)
- **Log when business rules prevent operations**
- **Include rule name and context** that caused the violation
- **Provide sufficient detail** for business analysis

#### 7. Event Sourcing Operations (MANDATORY)
- **All event append and read operations**
- **Include stream ID, event count, and position information**
- **Log both successful and failed operations**

#### 8. Performance-Critical Operations (MANDATORY)
- **Operations that take longer than 1 second**
- **Significant resource allocations** (> 10MB memory, database connections)
- **Batch processing operations** with item counts

### Decision Tree for Adding Logging

#### ALWAYS ADD LOGGING IF:
1. Method is public and part of a service interface
2. Method can throw an exception
3. Method performs I/O operations (database, file, network)
4. Method modifies data (create, update, delete)
5. Method calls external services
6. Method is an Orleans grain method
7. Method implements business logic with rules/validation
8. Method takes longer than 1 second to execute
9. Method allocates significant resources (> 10MB memory, database connections)
10. Method is part of the event sourcing pipeline

#### CONSIDER ADDING LOGGING IF:
1. Method is complex with multiple decision points
2. Method is frequently called and performance matters
3. Method is part of a critical business process
4. Method handles user input or external data
5. Method is difficult to test or debug

#### USUALLY DON'T LOG:
1. Simple getters/setters
2. Constructors (unless they do significant work)
3. Private helper methods that are single-purpose
4. Property accessors
5. Methods that only transform data without side effects

### MANDATORY Logging Checklist for Code Reviews

When reviewing code, ensure logging is present for:

- [ ] All public service methods have entry/exit logging
- [ ] All catch blocks have exception logging with context
- [ ] All data mutations (CRUD operations) are logged
- [ ] All external service calls are logged (start/success/failure)
- [ ] All Orleans grain methods have lifecycle and method logging
- [ ] All business rule violations are logged with context
- [ ] All event sourcing operations are logged
- [ ] Long-running operations (>1s) have performance logging
- [ ] Resource allocations (>10MB) are logged
- [ ] Inter-grain communication is logged

### Log Levels and Usage

#### Error Level
- **Application errors** that require immediate attention
- **Unhandled exceptions** and system failures
- **Business rule violations** that prevent normal operation
- **External service failures** that impact functionality

```csharp
// MANDATORY PATTERN: High-performance logging with LoggerMessage and LoggerExtensions class
public static class EventProcessingLoggerExtensions
{
    private static readonly Action<ILogger, string, string, Exception> EventProcessingFailedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(1, nameof(EventProcessingFailed)),
            "Failed to process event {EventId} for stream {StreamId}");

    public static void EventProcessingFailed(this ILogger<EventProcessingGrain> logger, string eventId, string streamId, Exception ex) =>
        EventProcessingFailedMessage(logger, eventId, streamId, ex);
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
// MANDATORY PATTERN: LoggerExtensions class with high-performance LoggerMessage
public static class SystemMonitoringLoggerExtensions
{
    private static readonly Action<ILogger, int, Exception?> HighMemoryUsageDetectedMessage =
        LoggerMessage.Define<int>(
            LogLevel.Warning,
            new EventId(1, nameof(HighMemoryUsageDetected)),
            "High memory usage detected: {MemoryUsage}MB");

    public static void HighMemoryUsageDetected(this ILogger<SystemMonitoringService> logger, int memoryUsage) =>
        HighMemoryUsageDetectedMessage(logger, memoryUsage, null);
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
// High-performance logging with LoggerMessage
public static class EventSourcingLoggerExtensions
{
    private static readonly Action<ILogger, string, string, long, Exception?> EventAppendedMessage =
        LoggerMessage.Define<string, string, long>(
            LogLevel.Information,
            new EventId(1, nameof(EventAppended)),
            "Event {EventId} appended to stream {StreamId} at position {Position}");

    public static void EventAppended(this ILogger<EventSourcingService> logger, string eventId, string streamId, long position) =>
        EventAppendedMessage(logger, eventId, streamId, position, null);
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
// High-performance logging with LoggerMessage
public static class EventProcessingLoggerExtensions
{
    private static readonly Action<ILogger, int, string, Exception?> ProcessingBatchMessage =
        LoggerMessage.Define<int, string>(
            LogLevel.Debug,
            new EventId(2, nameof(ProcessingBatch)),
            "Processing batch of {Count} events for stream {StreamId}");

    public static void ProcessingBatch(this ILogger<EventProcessingGrain> logger, int eventCount, string streamId) =>
        ProcessingBatchMessage(logger, eventCount, streamId, null);
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
// High-performance logging with LoggerMessage
public static class MemoryAllocationLoggerExtensions
{
    private static readonly Action<ILogger, int, Exception?> BufferAllocatedMessage =
        LoggerMessage.Define<int>(
            LogLevel.Trace,
            new EventId(1, nameof(BufferAllocated)),
            "Allocated {Bytes} bytes for event buffer");

    public static void BufferAllocated(this ILogger<MemoryAllocationService> logger, int bufferSize) =>
        BufferAllocatedMessage(logger, bufferSize, null);
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
// High-performance logging with LoggerMessage
public static class EventProcessingLoggerExtensions
{
    private static readonly Action<ILogger, string, Exception?> EventProcessedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, nameof(EventProcessed)),
            "EVT-001: Event {EventId} processed successfully");

    private static readonly Action<ILogger, string, string, Exception?> EventProcessedWithCorrelationMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(2, nameof(EventProcessedWithCorrelation)),
            "EVT-001: Event {EventId} processed successfully. CorrelationId: {CorrelationId}");

    public static void EventProcessed(this ILogger<EventProcessingGrain> logger, string eventId) =>
        EventProcessedMessage(logger, eventId, null);

    public static void EventProcessedWithCorrelation(this ILogger<EventProcessingGrain> logger, string eventId, string correlationId) =>
        EventProcessedWithCorrelationMessage(logger, eventId, correlationId, null);
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
// High-performance logging with LoggerMessage
public static class OrderProcessingLoggerExtensions
{
    private static readonly Action<ILogger, string, string, int, Exception?> OrderCreatedMessage =
        LoggerMessage.Define<string, string, int>(
            LogLevel.Information,
            new EventId(1, nameof(OrderCreated)),
            "User {UserId} created order {OrderId} with {ItemCount} items");

    private static readonly Action<ILogger, string, string, int, decimal, string, Exception?> OrderCreatedWithTotalMessage =
        LoggerMessage.Define<string, string, int, decimal, string>(
            LogLevel.Information,
            new EventId(2, nameof(OrderCreatedWithTotal)),
            "User {UserId} created order {OrderId} with {ItemCount} items. Total: {TotalAmount:C}, Currency: {Currency}");

    private static readonly Action<ILogger, string, string, int, decimal, string, string, string, Exception?> OrderCreatedCompleteMessage =
        LoggerMessage.Define<string, string, int, decimal, string, string, string>(
            LogLevel.Information,
            new EventId(3, nameof(OrderCreatedComplete)),
            "E-commerce order creation completed successfully. User: {UserId}, Order: {OrderId}, Items: {ItemCount}, Total: {TotalAmount:C}, Currency: {Currency}, PaymentMethod: {PaymentMethod}, ShippingAddress: {ShippingCountry}");

    public static void OrderCreated(this ILogger<OrderProcessingService> logger, string userId, string orderId, int itemCount) =>
        OrderCreatedMessage(logger, userId, orderId, itemCount, null);

    public static void OrderCreatedWithTotal(this ILogger<OrderProcessingService> logger, string userId, string orderId, int itemCount, decimal totalAmount, string currency) =>
        OrderCreatedWithTotalMessage(logger, userId, orderId, itemCount, totalAmount, currency, null);

    public static void OrderCreatedComplete(this ILogger<OrderProcessingService> logger, string userId, string orderId, int itemCount, decimal totalAmount, string currency, string paymentMethod, string shippingCountry) =>
        OrderCreatedCompleteMessage(logger, userId, orderId, itemCount, totalAmount, currency, paymentMethod, shippingCountry, null);
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
// High-performance logging with LoggerMessage
public static class EventProcessingLoggerExtensions
{
    private static readonly Action<ILogger, string, string, string, Exception> EventProcessingFailedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(3, nameof(EventProcessingFailed)),
            "Failed to process event {EventId} for stream {StreamId}. CorrelationId: {CorrelationId}");

    private static readonly Action<ILogger, string, string, string, string, string, string, int, Exception> EventProcessingFailedDetailedMessage =
        LoggerMessage.Define<string, string, string, string, string, string, int>(
            LogLevel.Error,
            new EventId(4, nameof(EventProcessingFailedDetailed)),
            "Event processing failed during stream operation. EventId: {EventId}, StreamId: {StreamId}, EventType: {EventType}, EventVersion: {EventVersion}, CorrelationId: {CorrelationId}, ProcessingStep: {ProcessingStep}, RetryCount: {RetryCount}");

    public static void EventProcessingFailed(this ILogger<EventProcessingGrain> logger, string eventId, string streamId, string correlationId, Exception ex) =>
        EventProcessingFailedMessage(logger, eventId, streamId, correlationId, ex);

    public static void EventProcessingFailedDetailed(this ILogger<EventProcessingGrain> logger, string eventId, string streamId, string eventType, string eventVersion, string correlationId, string processingStep, int retryCount, Exception ex) =>
        EventProcessingFailedDetailedMessage(logger, eventId, streamId, eventType, eventVersion, correlationId, processingStep, retryCount, ex);
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
- **MANDATORY: Always use LoggerMessage for ALL logging** - Direct ILogger calls are FORBIDDEN - reduces allocations and parsing overhead
- **MANDATORY: Use LoggerExtensions classes** - ALL logging must be implemented in `public static class [ComponentName]LoggerExtensions` classes
- **Define static Action delegates** - Cache log message templates for better performance as private static readonly fields
- **Use strongly-typed parameters** - Avoid boxing of value types
- **Leverage source generation** - Use LoggerMessageAttribute in .NET 6+ for compile-time optimization
- **NO EXCEPTIONS** - This pattern is required for all logging, without exception

```csharp
// MANDATORY PATTERN: LoggerExtensions class with high-performance LoggerMessage
public static class EventProcessorLoggerExtensions
{
    private static readonly Action<ILogger, string, string, Exception?> EventProcessedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1, nameof(EventProcessed)),
            "Event {EventId} processed for stream {StreamId}");

    private static readonly Action<ILogger, string, string, Exception> EventProcessingFailedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(2, nameof(EventProcessingFailed)),
            "Failed to process event {EventId} for stream {StreamId}");

    public static void EventProcessed(this ILogger<EventProcessorGrain> logger, string eventId, string streamId) =>
        EventProcessedMessage(logger, eventId, streamId, null);

    public static void EventProcessingFailed(this ILogger<EventProcessorGrain> logger, string eventId, string streamId, Exception ex) =>
        EventProcessingFailedMessage(logger, eventId, streamId, ex);
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
// MANDATORY PATTERN: LoggerExtensions class with high-performance LoggerMessage
public static class PerformanceServiceLoggerExtensions
{
    private static readonly Action<ILogger, string, Exception?> ExpensiveOperationResultMessage =
        LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(1, nameof(ExpensiveOperationResult)),
            "Expensive operation result: {Result}");

    private static readonly Action<ILogger, object, Exception?> UserDataLoggedMessage =
        LoggerMessage.Define<object>(
            LogLevel.Debug,
            new EventId(2, nameof(UserDataLogged)),
            "User data: {@UserData}");

    public static void ExpensiveOperationResult(this ILogger<PerformanceService> logger, string result) =>
        ExpensiveOperationResultMessage(logger, result, null);

    public static void UserDataLogged(this ILogger<PerformanceService> logger, object userData) =>
        UserDataLoggedMessage(logger, userData, null);
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
// High-performance logging with LoggerMessage
public static class SecurityLoggerExtensions
{
    private static readonly Action<ILogger, string, Exception?> UserAuthenticatedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, nameof(UserAuthenticated)),
            "User {UserId} authenticated. Token: [REDACTED]");

    private static readonly Action<ILogger, Exception?> PiiDataRedactedMessage =
        LoggerMessage.Define(
            LogLevel.Information,
            new EventId(2, nameof(PiiDataRedacted)),
            "Processing user data. PII fields redacted for privacy compliance");

    public static void UserAuthenticated(this ILogger<SecurityService> logger, string userId) =>
        UserAuthenticatedMessage(logger, userId, null);

    public static void PiiDataRedacted(this ILogger<SecurityService> logger) =>
        PiiDataRedactedMessage(logger, null);
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
// High-performance logging with LoggerMessage
public static class OperationLoggerExtensions
{
    private static readonly Action<ILogger, string, Exception?> OperationStartedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, nameof(OperationStarted)),
            "Starting operation. CorrelationId: {CorrelationId}");

    private static readonly Action<ILogger, string, string, string, string, string, Exception?> AuthenticationWorkflowStartedMessage =
        LoggerMessage.Define<string, string, string, string, string>(
            LogLevel.Information,
            new EventId(2, nameof(AuthenticationWorkflowStarted)),
            "Starting user authentication workflow. Operation: {Operation}, UserId: {UserId}, AuthMethod: {AuthMethod}, CorrelationId: {CorrelationId}, Environment: {Environment}");

    public static void OperationStarted(this ILogger<OperationService> logger, string correlationId) =>
        OperationStartedMessage(logger, correlationId, null);

    public static void AuthenticationWorkflowStarted(this ILogger<OperationService> logger, string operation, string userId, string authMethod, string correlationId, string environment) =>
        AuthenticationWorkflowStartedMessage(logger, operation, userId, authMethod, correlationId, environment, null);
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
// High-performance logging with LoggerMessage
public static class HttpRequestLoggerExtensions
{
    private static readonly Action<ILogger, string, string, long, string, string, Exception?> HttpRequestCompletedMessage =
        LoggerMessage.Define<string, string, long, string, string>(
            LogLevel.Information,
            new EventId(1, nameof(HttpRequestCompleted)),
            "HTTP {Method} {Path} completed in {Duration}ms. User: {UserId}, Tenant: {TenantId}");

    private static readonly Action<ILogger, string, string, int, long, string, string, string, string, long, long, Exception?> HttpRequestCompletedDetailedMessage =
        LoggerMessage.Define<string, string, int, long, string, string, string, string, long, long>(
            LogLevel.Information,
            new EventId(2, nameof(HttpRequestCompletedDetailed)),
            "HTTP request completed successfully. Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, Duration: {Duration}ms, User: {UserId}, Tenant: {TenantId}, UserAgent: {UserAgent}, IPAddress: {IPAddress}, RequestSize: {RequestSize}bytes, ResponseSize: {ResponseSize}bytes");

    public static void HttpRequestCompleted(this ILogger<HttpRequestMiddleware> logger, string method, string path, long duration, string userId, string tenantId) =>
        HttpRequestCompletedMessage(logger, method, path, duration, userId, tenantId, null);

    public static void HttpRequestCompletedDetailed(this ILogger<HttpRequestMiddleware> logger, string method, string path, int statusCode, long duration, string userId, string tenantId, string userAgent, string ipAddress, long requestSize, long responseSize) =>
        HttpRequestCompletedDetailedMessage(logger, method, path, statusCode, duration, userId, tenantId, userAgent, ipAddress, requestSize, responseSize, null);
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
// High-performance logging with LoggerMessage
public static class EventSourcingOperationsLoggerExtensions
{
    private static readonly Action<ILogger, int, string, long, string, Exception?> EventsAppendedMessage =
        LoggerMessage.Define<int, string, long, string>(
            LogLevel.Information,
            new EventId(1, nameof(EventsAppended)),
            "EVT-APPEND: Appended {EventCount} events to stream {StreamId} at position {Position}. CorrelationId: {CorrelationId}");

    private static readonly Action<ILogger, int, string, long, long, string, Exception?> EventsReadMessage =
        LoggerMessage.Define<int, string, long, long, string>(
            LogLevel.Information,
            new EventId(2, nameof(EventsRead)),
            "EVT-READ: Read {EventCount} events from stream {StreamId} positions {StartPosition}-{EndPosition}. CorrelationId: {CorrelationId}");

    public static void EventsAppended(this ILogger<EventSourcingService> logger, int eventCount, string streamId, long position, string correlationId) =>
        EventsAppendedMessage(logger, eventCount, streamId, position, correlationId, null);

    public static void EventsRead(this ILogger<EventSourcingService> logger, int eventCount, string streamId, long startPosition, long endPosition, string correlationId) =>
        EventsReadMessage(logger, eventCount, streamId, startPosition, endPosition, correlationId, null);
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
// High-performance logging with LoggerMessage
public static class EventSourcingGrainLoggerExtensions
{
    private static readonly Action<ILogger, string, string, string, Exception?> GrainActivatedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(1, nameof(GrainActivated)),
            "GRAIN-ACTIVATE: Grain {GrainType} activated with ID {GrainId}. PrimaryKey: {PrimaryKey}");

    private static readonly Action<ILogger, string, string, string, Exception?> GrainDeactivatedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(2, nameof(GrainDeactivated)),
            "GRAIN-DEACTIVATE: Grain {GrainType} deactivated with ID {GrainId}. Reason: {DeactivationReason}");

    private static readonly Action<ILogger, string, string, string, string, Exception?> GrainMethodStartedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Information,
            new EventId(3, nameof(GrainMethodStarted)),
            "GRAIN-METHOD-START: {MethodName} called on grain {GrainId}. EventId: {EventId}, CorrelationId: {CorrelationId}");

    private static readonly Action<ILogger, string, string, long, string, Exception?> GrainMethodSuccessMessage =
        LoggerMessage.Define<string, string, long, string>(
            LogLevel.Information,
            new EventId(4, nameof(GrainMethodSuccess)),
            "GRAIN-METHOD-SUCCESS: {MethodName} completed on grain {GrainId}. Duration: {Duration}ms, EventId: {EventId}");

    private static readonly Action<ILogger, string, string, long, string, string, Exception> GrainMethodErrorMessage =
        LoggerMessage.Define<string, string, long, string, string>(
            LogLevel.Error,
            new EventId(5, nameof(GrainMethodError)),
            "GRAIN-METHOD-ERROR: {MethodName} failed on grain {GrainId}. Duration: {Duration}ms, EventId: {EventId}, CorrelationId: {CorrelationId}");

    public static void GrainActivated(this ILogger<EventSourcingGrain> logger, string grainType, string grainId, string primaryKey) =>
        GrainActivatedMessage(logger, grainType, grainId, primaryKey, null);

    public static void GrainDeactivated(this ILogger<EventSourcingGrain> logger, string grainType, string grainId, string deactivationReason) =>
        GrainDeactivatedMessage(logger, grainType, grainId, deactivationReason, null);

    public static void GrainMethodStarted(this ILogger<EventSourcingGrain> logger, string methodName, string grainId, string eventId, string correlationId) =>
        GrainMethodStartedMessage(logger, methodName, grainId, eventId, correlationId, null);

    public static void GrainMethodSuccess(this ILogger<EventSourcingGrain> logger, string methodName, string grainId, long duration, string eventId) =>
        GrainMethodSuccessMessage(logger, methodName, grainId, duration, eventId, null);

    public static void GrainMethodError(this ILogger<EventSourcingGrain> logger, string methodName, string grainId, long duration, string eventId, string correlationId, Exception ex) =>
        GrainMethodErrorMessage(logger, methodName, grainId, duration, eventId, correlationId, ex);
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
// High-performance logging with LoggerMessage for Orleans correlation handling
public static class OrleansCorrelationLoggerExtensions
{
    private static readonly Action<ILogger, string, string, string, Exception?> CorrelationIdPropagatedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            new EventId(1, nameof(CorrelationIdPropagated)),
            "Correlation ID {CorrelationId} propagated from grain {SourceGrain} to {TargetGrain}");

    private static readonly Action<ILogger, string, string, Exception?> CorrelationIdCreatedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(2, nameof(CorrelationIdCreated)),
            "New correlation ID {CorrelationId} created for grain {GrainType}");

    private static readonly Action<ILogger, string, string, string, Exception?> GrainCallStartedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            new EventId(3, nameof(GrainCallStarted)),
            "Grain call started. Method: {MethodName}, Target: {TargetGrain}, CorrelationId: {CorrelationId}");

    private static readonly Action<ILogger, string, string, string, long, Exception?> GrainCallCompletedMessage =
        LoggerMessage.Define<string, string, string, long>(
            LogLevel.Debug,
            new EventId(4, nameof(GrainCallCompleted)),
            "Grain call completed. Method: {MethodName}, Target: {TargetGrain}, CorrelationId: {CorrelationId}, Duration: {Duration}ms");

    private static readonly Action<ILogger, string, string, string, long, string, Exception> GrainCallFailedMessage =
        LoggerMessage.Define<string, string, string, long, string>(
            LogLevel.Error,
            new EventId(5, nameof(GrainCallFailed)),
            "Grain call failed. Method: {MethodName}, Target: {TargetGrain}, CorrelationId: {CorrelationId}, Duration: {Duration}ms, Error: {Error}");

    public static void CorrelationIdPropagated(this ILogger<OrleansCorrelationHelper> logger, string correlationId, string sourceGrain, string targetGrain) =>
        CorrelationIdPropagatedMessage(logger, correlationId, sourceGrain, targetGrain, null);

    public static void CorrelationIdCreated(this ILogger<OrleansCorrelationHelper> logger, string correlationId, string grainType) =>
        CorrelationIdCreatedMessage(logger, correlationId, grainType, null);

    public static void GrainCallStarted(this ILogger<OrleansCorrelationHelper> logger, string methodName, string targetGrain, string correlationId) =>
        GrainCallStartedMessage(logger, methodName, targetGrain, correlationId, null);

    public static void GrainCallCompleted(this ILogger<OrleansCorrelationHelper> logger, string methodName, string targetGrain, string correlationId, long duration) =>
        GrainCallCompletedMessage(logger, methodName, targetGrain, correlationId, duration, null);

    public static void GrainCallFailed(this ILogger<OrleansCorrelationHelper> logger, string methodName, string targetGrain, string correlationId, long duration, string error, Exception ex) =>
        GrainCallFailedMessage(logger, methodName, targetGrain, correlationId, duration, error, ex);
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
// High-performance logging with LoggerMessage
public static class CosmosDbLoggerExtensions
{
    private static readonly Action<ILogger, string, string, double, long, string, Exception?> CosmosOperationMessage =
        LoggerMessage.Define<string, string, double, long, string>(
            LogLevel.Information,
            new EventId(1, nameof(CosmosOperation)),
            "COSMOS-OP: {Operation} on container {Container}. RequestUnits: {RequestUnits}, Duration: {Duration}ms. CorrelationId: {CorrelationId}");

    private static readonly Action<ILogger, int, string, long, string, Exception?> CosmosRetryMessage =
        LoggerMessage.Define<int, string, long, string>(
            LogLevel.Warning,
            new EventId(2, nameof(CosmosRetry)),
            "COSMOS-RETRY: Retry {RetryCount} for operation {Operation}. Backoff: {BackoffMs}ms. CorrelationId: {CorrelationId}");

    public static void CosmosOperation(this ILogger<CosmosDbService> logger, string operation, string container, double requestUnits, long duration, string correlationId) =>
        CosmosOperationMessage(logger, operation, container, requestUnits, duration, correlationId, null);

    public static void CosmosRetry(this ILogger<CosmosDbService> logger, int retryCount, string operation, long backoffMs, string correlationId) =>
        CosmosRetryMessage(logger, retryCount, operation, backoffMs, correlationId, null);
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
// High-performance logging with LoggerMessage
public static class CorrelationIdMiddlewareLoggerExtensions
{
    private static readonly Action<ILogger, string, Exception?> RequestStartedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, nameof(RequestStarted)),
            "Request started. CorrelationId: {CorrelationId}");

    public static void RequestStarted(this ILogger<CorrelationIdMiddleware> logger, string correlationId) =>
        RequestStartedMessage(logger, correlationId, null);
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
- [ ] **MANDATORY: ALL logging uses LoggerExtensions classes** - Must follow `public static class [ComponentName]LoggerExtensions` pattern
- [ ] **MANDATORY: NO direct ILogger calls** - All logging must go through static extension methods
- [ ] **MANDATORY: Uses LoggerMessage pattern** - All logging must use static Action delegates with LoggerMessage.Define
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
public static class OrderServiceLoggerExtensions
{
    private static readonly Action<ILogger, string, string, Exception?> ProcessOrderStartedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1, nameof(ProcessOrderStarted)),
            "Processing order {OrderId} for customer {CustomerId}");

    private static readonly Action<ILogger, string, string, Exception?> ProcessOrderCompletedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(2, nameof(ProcessOrderCompleted)),
            "Order {OrderId} processed successfully with status {Status}");

    private static readonly Action<ILogger, string, Exception> ProcessOrderFailedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3, nameof(ProcessOrderFailed)),
            "Failed to process order {OrderId}");

    public static void ProcessOrderStarted(this ILogger<OrderService> logger, string orderId, string customerId) =>
        ProcessOrderStartedMessage(logger, orderId, customerId, null);

    public static void ProcessOrderCompleted(this ILogger<OrderService> logger, string orderId, string status) =>
        ProcessOrderCompletedMessage(logger, orderId, status, null);

    public static void ProcessOrderFailed(this ILogger<OrderService> logger, string orderId, Exception ex) =>
        ProcessOrderFailedMessage(logger, orderId, ex);
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
public static class UserRepositoryLoggerExtensions
{
    private static readonly Action<ILogger, string, string, Exception?> UserCreationStartedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1, nameof(UserCreationStarted)),
            "Creating user {UserId} with email {Email}");

    private static readonly Action<ILogger, string, Exception?> UserCreatedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(2, nameof(UserCreated)),
            "User {UserId} created successfully");

    private static readonly Action<ILogger, string, Exception> UserCreationFailedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3, nameof(UserCreationFailed)),
            "Failed to create user {UserId}");

    public static void UserCreationStarted(this ILogger<UserRepository> logger, string userId, string email) =>
        UserCreationStartedMessage(logger, userId, email, null);

    public static void UserCreated(this ILogger<UserRepository> logger, string userId) =>
        UserCreatedMessage(logger, userId, null);

    public static void UserCreationFailed(this ILogger<UserRepository> logger, string userId, Exception ex) =>
        UserCreationFailedMessage(logger, userId, ex);
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
public static class EventProcessingGrainLoggerExtensions
{
    private static readonly Action<ILogger, string, string, Exception?> GrainActivatedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1, nameof(GrainActivated)),
            "Grain {GrainType} activated with ID {GrainId}");

    private static readonly Action<ILogger, string, string, string, Exception?> GrainMethodStartedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(2, nameof(GrainMethodStarted)),
            "Method {MethodName} started on grain {GrainId} for event {EventId}");

    private static readonly Action<ILogger, string, string, string, long, Exception?> GrainMethodCompletedMessage =
        LoggerMessage.Define<string, string, string, long>(
            LogLevel.Information,
            new EventId(3, nameof(GrainMethodCompleted)),
            "Method {MethodName} completed on grain {GrainId} for event {EventId} in {Duration}ms");

    public static void GrainActivated(this ILogger<EventProcessingGrain> logger, string grainType, string grainId) =>
        GrainActivatedMessage(logger, grainType, grainId, null);

    public static void GrainMethodStarted(this ILogger<EventProcessingGrain> logger, string methodName, string grainId, string eventId) =>
        GrainMethodStartedMessage(logger, methodName, grainId, eventId, null);

    public static void GrainMethodCompleted(this ILogger<EventProcessingGrain> logger, string methodName, string grainId, string eventId, long duration) =>
        GrainMethodCompletedMessage(logger, methodName, grainId, eventId, duration, null);
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
// MANDATORY PATTERN: High-performance logging with LoggerMessage and LoggerExtensions class
public static class ExampleServiceLoggerExtensions
{
    private static readonly Action<ILogger, int, string, Exception?> DataProcessingStartedMessage =
        LoggerMessage.Define<int, string>(
            LogLevel.Information,
            new EventId(1, nameof(DataProcessingStarted)),
            "PROC-001: Starting data processing. DataLength: {DataLength}, CorrelationId: {CorrelationId}");

    private static readonly Action<ILogger, string, string, Exception?> DataProcessingCompletedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(2, nameof(DataProcessingCompleted)),
            "PROC-002: Data processing completed successfully. Result: {Result}, CorrelationId: {CorrelationId}");

    private static readonly Action<ILogger, int, string, Exception> DataProcessingFailedMessage =
        LoggerMessage.Define<int, string>(
            LogLevel.Error,
            new EventId(3, nameof(DataProcessingFailed)),
            "PROC-ERROR: Data processing failed. DataLength: {DataLength}, CorrelationId: {CorrelationId}");

    public static void DataProcessingStarted(this ILogger<ExampleService> logger, int dataLength, string correlationId) =>
        DataProcessingStartedMessage(logger, dataLength, correlationId, null);

    public static void DataProcessingCompleted(this ILogger<ExampleService> logger, string result, string correlationId) =>
        DataProcessingCompletedMessage(logger, result, correlationId, null);

    public static void DataProcessingFailed(this ILogger<ExampleService> logger, int dataLength, string correlationId, Exception ex) =>
        DataProcessingFailedMessage(logger, dataLength, correlationId, ex);
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
1. **MANDATORY: LoggerExtensions Classes** - ALL logging must be implemented using `public static class [ComponentName]LoggerExtensions` pattern
2. **MANDATORY: LoggerMessage Pattern** - ALL logging must use static Action delegates with LoggerMessage.Define
3. **FORBIDDEN: Direct ILogger calls** - NO direct ILogger.Log*() method calls are allowed anywhere in the codebase
4. **MANDATORY: High-Performance Pattern** - This is not optional - it's required for all logging without exception

### Behavioral Requirements (WHEN to log)
5. **MANDATORY: Public Service Methods** - ALL public service methods must have entry/exit logging
6. **MANDATORY: Exception Handling** - ALL catch blocks must log exceptions with context
7. **MANDATORY: Data Mutations** - ALL create/update/delete operations must be logged
8. **MANDATORY: External Service Calls** - ALL external service interactions must be logged
9. **MANDATORY: Orleans Grain Operations** - ALL grain methods and lifecycle events must be logged
10. **MANDATORY: Business Rule Violations** - ALL business rule failures must be logged with context

### Agent Implementation Rule
When an AI agent encounters any of the mandatory scenarios above (#5-10), it MUST add appropriate logging following the LoggerExtensions pattern (#1-4). This ensures deterministic behavior and complete observability.

Failure to follow these patterns will result in code review rejection and build failures.

## Related Guidelines

This document should be read in conjunction with:

- **C# General Development Best Practices** (`.github/instructions/csharp.instructions.md`) - For SOLID principles, dependency injection property patterns, and access control principles
- **Service Registration and Configuration** (`.github/instructions/service-registration.instructions.md`) - For IHostedService logging patterns, Orleans lifecycle participant logging, and configuration validation logging
- **Orleans Best Practices** (`.github/instructions/orleans.instructions.md`) - For Orleans grain logging patterns, POCO grain requirements, and grain lifecycle logging
- **Orleans Serialization** (`.github/instructions/orleans-serialization.instructions.md`) - For Orleans-specific logging during serialization operations
- **Build Rules** (`.github/instructions/build-rules.instructions.md`) - For quality standards, zero warnings policy, and build pipeline requirements that enforce logging standards
- **Naming Conventions** (`.github/instructions/naming.instructions.md`) - For LoggerExtensions class naming patterns, XML documentation requirements, and structured logging property naming

## References

- [High-performance logging in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging) - Microsoft documentation on LoggerMessage and performance optimization
- [Microsoft Orleans Documentation](https://learn.microsoft.com/en-us/dotnet/orleans/) - Orleans-specific patterns and best practices
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/) - Vendor-neutral observability framework
