---
applyTo: '**/*.cs'
---

# C# General Development Best Practices

This document codifies our baseline engineering expectations for any C# service or component within the Mississippi Framework and related applications. These practices ensure consistency, maintainability, and alignment with industry standards across all C# codebases.

## Core Principles

- **Always apply SOLID** — keep asking whether each class has *only one* reason to change.
- **Code must remain unit-testable** — leverage dependency injection, side-effect-free functions, and clear seams so every unit can be isolated.
- **Stick to Microsoft libraries / NuGet packages** — add third-party dependencies only with explicit approval or when they extend tech already in use (e.g., an Orleans storage provider).
- **Principle priority: SOLID → DRY → KISS** — remove duplication first, then keep the solution as simple as possible.
- **Follow Microsoft best practices** wherever official guidance exists (coding conventions, security baselines, project layout, etc.).
- **Prefer cloud-native designs** — build for statelessness, horizontal scaling, and managed services.
- **Favor immutable objects—pragmatically** — default to C# *records* or `init`-only properties for domain models, events, and public contracts; relax the rule only when there's a clear, justified need.
- **Keep .NET analyzers enabled** — treat most warnings as build blockers to maintain hygiene.
- **Use dependency injection with get-only properties** — follow the `private Type Name { get; }` pattern for all injected dependencies, consistent with logging best practices.
- **If Microsoft Orleans is in the project, think like a grain** — avoid blocking calls, shared mutable state, and chatty inter-grain traffic; design for Orleans' single-threaded scheduler, placement, and storage semantics.
- **Never use `Parallel.ForEach`** — assume any code might execute inside an Orleans grain; instead use `await` + `Task.WhenAll` for fan-out concurrency.

## Access Control and Encapsulation Principles

- **Classes are `sealed` by default** — only make classes inheritable when you have a clear, documented need for polymorphism; prefer composition over inheritance
- **Members are `private` by default** — only expose what consumers actually need; prefer `internal` over `public` when sharing within assemblies
- **Interfaces are `internal` by default** — only make interfaces `public` when they're part of your deliberate API surface
- **Orleans grain interfaces access** — grain interfaces are `public` when consumed by external clients; keep `internal` when used only intra-assembly
- **Explicit access control decisions** — every `public`, `protected`, or unsealed class must have a documented justification in XML comments
- **Principle of least privilege** — grant the minimum access level required for the intended use case; you can always widen access later, but narrowing it is a breaking change
- **Follow Orleans POCO pattern** — all grain classes must be `sealed` and implement `IGrainBase` instead of inheriting from `Grain`

## Service Registration and Configuration

- **Use dedicated service registration pattern** — follow hierarchical feature-based registration using extension methods as defined in the service registration guidelines
- **Always use Options pattern for configuration** — NEVER use direct configuration parameters in constructors; always use `IOptions<T>`, `IOptionsSnapshot<T>`, or `IOptionsMonitor<T>`
- **Follow dependency injection property pattern** — all registered services must use `private Type Name { get; }` pattern when injected, consistent with logging and Orleans best practices

## Twelve-Factor Checklist (Required for Cloud-Native Design)

1. **Codebase** – one codebase, many deploys
2. **Dependencies** – declare & isolate all dependencies
3. **Config** – store config in the environment
4. **Backing services** – treat backing services as attached resources
5. **Build, release, run** – strictly separate stages
6. **Processes** – run as stateless processes
7. **Port binding** – self-host via port binding
8. **Concurrency** – scale out with the process model
9. **Disposability** – fast start-up / graceful shutdown
10. **Dev-prod parity** – keep environments similar
11. **Logs** – treat logs as event streams
12. **Admin processes** – run one-off tasks as scripts

## Code Examples

### SOLID Principles Implementation

```csharp
// Single Responsibility Principle - Each class has one reason to change
public class OrderValidator
{
    public ValidationResult Validate(Order order)
    {
        // Only validates orders - doesn't process, save, or format them
    }
}

public class OrderProcessor
{
    private IOrderRepository Repository { get; }
    private IOrderValidator Validator { get; }
    
    public OrderProcessor(IOrderRepository repository, IOrderValidator validator)
    {
        Repository = repository;
        Validator = validator;
    }
    
    // Only processes orders - doesn't validate or persist them directly
}
```

### Dependency Injection Pattern

```csharp
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}

// High-performance logging extensions following logging rules
public static class EmailServiceLoggerExtensions
{
    private static readonly Action<ILogger, string, string, Exception?> s_emailSent =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1, nameof(EmailSent)),
            "Email sent successfully to {Recipient} with subject {Subject}");

    private static readonly Action<ILogger, string, string, Exception> s_emailFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(2, nameof(EmailFailed)),
            "Failed to send email to {Recipient} with subject {Subject}");

    public static void EmailSent(this ILogger<EmailService> logger, string recipient, string subject) =>
        s_emailSent(logger, recipient, subject, null);

    public static void EmailFailed(this ILogger<EmailService> logger, string recipient, string subject, Exception ex) =>
        s_emailFailed(logger, recipient, subject, ex);
}

public class EmailService : IEmailService
{
    private ILogger<EmailService> Logger { get; }
    private IOptions<EmailSettings> Settings { get; }
    
    public EmailService(ILogger<EmailService> logger, IOptions<EmailSettings> settings)
    {
        Logger = logger;
        Settings = settings;
    }
    
    public async Task SendAsync(string to, string subject, string body)
    {
        try
        {
            // Implementation that can be easily unit tested
            await SendEmailInternalAsync(to, subject, body);
            Logger.EmailSent(to, subject);
        }
        catch (Exception ex)
        {
            Logger.EmailFailed(to, subject, ex);
            throw;
        }
    }
    
    private async Task SendEmailInternalAsync(string to, string subject, string body)
    {
        // Actual email sending implementation
    }
}
```

### Immutable Objects with Records

```csharp
// Domain model using records for immutability
public record Customer(
    string Id,
    string Name,
    string Email,
    DateTime CreatedAt)
{
    // Init-only properties for optional fields
    public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; } = true;
}

// Event using record for immutable event sourcing
public record CustomerCreatedEvent(
    string CustomerId,
    string Name,
    string Email,
    DateTime OccurredAt) : IDomainEvent;
```

### Orleans-Safe Concurrency

```csharp
// ❌ NEVER use Parallel.ForEach in Orleans context
public async Task ProcessItemsBad(IEnumerable<string> items)
{
    Parallel.ForEach(items, item =>
    {
        ProcessItem(item); // This breaks Orleans single-threaded model
    });
}

// ✅ Use Task.WhenAll for concurrent processing
public async Task ProcessItemsGood(IEnumerable<string> items)
{
    var tasks = items.Select(async item => await ProcessItemAsync(item));
    await Task.WhenAll(tasks);
}
```

### Access Control and Encapsulation Examples

```csharp
namespace Mississippi.EventSourcing.Streams
{
    /// <summary>
    /// Processes event streams with Orleans-safe patterns and proper access control.
    /// This class is sealed to prevent inheritance - composition is preferred.
    /// Made public as it represents a key product boundary for the EventSourcing feature.
    /// </summary>
    public sealed class EventStreamProcessor : IGrainBase, IEventStreamProcessor, IDisposable
    {
        public IGrainContext GrainContext { get; }
        
        // Following mandatory dependency injection property pattern
        private ILogger<EventStreamProcessor> Logger { get; }
        private IEventStore EventStore { get; }
        private IOptions<StreamSettings> Settings { get; }
        
        // Private field following camelCase naming (no underscores)
        private readonly CancellationTokenSource cancellationTokenSource;
        
        public EventStreamProcessor(
            IGrainContext grainContext,
            ILogger<EventStreamProcessor> logger,
            IEventStore eventStore,
            IOptions<StreamSettings> settings)
        {
            GrainContext = grainContext;
            Logger = logger;
            EventStore = eventStore;
            Settings = settings;
            cancellationTokenSource = new CancellationTokenSource();
        }
        
        /// <summary>
        /// Gets a value indicating whether the processor is currently active.
        /// </summary>
        public bool IsActive { get; private set; }
        
        /// <summary>
        /// Processes events from the specified stream using Orleans-safe patterns.
        /// </summary>
        /// <param name="streamId">The unique identifier of the stream.</param>
        /// <param name="events">The events to process.</param>
        /// <returns>A task representing the processing result.</returns>
        public async Task<ProcessingResult> ProcessEventsAsync(string streamId, IReadOnlyList<DomainEvent> events)
        {
            try
            {
                // Use dependency injection logger following logging rules
                Logger.EventProcessingStarted(streamId, events.Count);
                
                var result = await ProcessEventsInternalAsync(streamId, events);
                
                Logger.EventProcessingCompleted(streamId, result.ProcessedCount);
                return result;
            }
            catch (Exception ex)
            {
                Logger.EventProcessingFailed(streamId, events.Count, ex);
                throw;
            }
        }
        
        // Private helper method - not exposed beyond this class
        private async Task<ProcessingResult> ProcessEventsInternalAsync(string streamId, IReadOnlyList<DomainEvent> events)
        {
            // Implementation details kept private
            return new ProcessingResult { ProcessedCount = events.Count };
        }
        
        public void Dispose()
        {
            cancellationTokenSource?.Dispose();
        }
    }
    
    /// <summary>
    /// Internal interface for stream validation - not part of public API surface.
    /// Only exposed within the assembly for testing and internal composition.
    /// </summary>
    internal interface IStreamValidator
    {
        ValidationResult ValidateStream(string streamId);
    }
    
    /// <summary>
    /// Internal implementation detail for stream validation.
    /// Sealed to prevent inheritance and marked internal as it's not a product boundary.
    /// </summary>
    internal sealed class StreamValidator : IStreamValidator
    {
        private ILogger<StreamValidator> Logger { get; }
        
        public StreamValidator(ILogger<StreamValidator> logger)
        {
            Logger = logger;
        }
        
        public ValidationResult ValidateStream(string streamId)
        {
            // Implementation kept internal
            return ValidationResult.Success;
        }
    }
}
```

### Service Registration Pattern Implementation

```csharp
// Root level service registration - Mississippi.EventSourcing/ServiceRegistration.cs
namespace Mississippi.EventSourcing
{
    /// <summary>
    /// Provides service registration for the complete EventSourcing feature.
    /// This is a logical product boundary, so the registration method is public.
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers all EventSourcing services including streams, storage, and core components.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddEventSourcing(this IServiceCollection services)
        {
            return services
                .AddStreams()           // Calls child feature registration
                .AddEventSourcingCore() // This feature's core services
                .AddScoped<IEventBus, EventBus>()
                .AddSingleton<IEventStore, EventStore>();
        }
        
        /// <summary>
        /// Registers only the core EventSourcing services without storage providers.
        /// Public method as consumers might want just core functionality.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddEventSourcingCore(this IServiceCollection services)
        {
            return services
                .AddScoped<IEventSerializer, EventSerializer>()
                .AddScoped<IEventMetadataFactory, EventMetadataFactory>();
        }
    }
}

// Child feature registration - Mississippi.EventSourcing.Streams/ServiceRegistration.cs
namespace Mississippi.EventSourcing.Streams
{
    /// <summary>
    /// Provides service registration for stream processing capabilities.
    /// Internal class as this is an implementation detail of EventSourcing.
    /// </summary>
    internal static class ServiceRegistration
    {
        /// <summary>
        /// Registers stream processing services and their dependencies.
        /// Internal method as consumers shouldn't register streams independently.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        /// <returns>The service collection for method chaining.</returns>
        internal static IServiceCollection AddStreams(this IServiceCollection services)
        {
            return services
                .AddBatching()          // Calls sub-feature registration
                .AddScoped<IStreamProcessor, StreamProcessor>()
                .AddScoped<IStreamValidator, StreamValidator>()
                .Configure<StreamSettings>(options =>
                {
                    options.BatchSize = 100;
                    options.MaxConcurrency = 10;
                });
        }
    }
}

// Sub-feature registration - Mississippi.EventSourcing.Streams.Batching/ServiceRegistration.cs  
namespace Mississippi.EventSourcing.Streams.Batching
{
    /// <summary>
    /// Provides service registration for batch processing within streams.
    /// Internal class as batching is an implementation detail of stream processing.
    /// </summary>
    internal static class ServiceRegistration
    {
        /// <summary>
        /// Registers batch processing services.
        /// Internal method as batching shouldn't be used without stream processing.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        /// <returns>The service collection for method chaining.</returns>
        internal static IServiceCollection AddBatching(this IServiceCollection services)
        {
            return services
                .AddScoped<IBatchProcessor, BatchProcessor>()
                .AddScoped<IBatchSizeCalculator, BatchSizeCalculator>()
                .Configure<BatchingOptions>(options =>
                {
                    options.MaxBatchSize = 1000;
                    options.TimeoutSeconds = 30;
                });
        }
    }
}

// Storage provider registration - Mississippi.EventSourcing.Cosmos/ServiceRegistration.cs
namespace Mississippi.EventSourcing.Cosmos
{
    /// <summary>
    /// Provides service registration for Cosmos DB storage implementation.
    /// Public class as this represents a logical product boundary for Cosmos storage.
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers EventSourcing with Cosmos DB storage provider.
        /// Public method as consumers choose their storage implementation.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        /// <param name="connectionString">The Cosmos DB connection string.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddEventSourcingCosmos(
            this IServiceCollection services, 
            string connectionString)
        {
            return services
                .AddEventSourcingCore()  // Include core without full EventSourcing
                .AddScoped<IBrookStorageProvider, CosmosStorageProvider>()
                .AddScoped<ICosmosRepository, CosmosRepository>()
                .Configure<CosmosOptions>(options =>
                {
                    options.ConnectionString = connectionString;
                    options.DatabaseName = "EventStore";
                })
                .AddSingleton<CosmosClient>(provider =>
                {
                    var options = provider.GetRequiredService<IOptions<CosmosOptions>>();
                    return new CosmosClient(options.Value.ConnectionString);
                });
        }
    }
}

// Usage examples showing proper consumption patterns
namespace SampleApplication
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var services = new ServiceCollection();
            
            // Option 1: Full EventSourcing with default storage
            services.AddEventSourcing();
            
            // Option 2: EventSourcing with specific Cosmos storage
            services.AddEventSourcingCosmos("cosmosdb-connection-string");
            
            // Option 3: Just core EventSourcing for custom implementations
            services.AddEventSourcingCore();
            
            // ❌ WRONG - Cannot register internal features directly
            // services.AddStreams();     // Compile error - internal method
            // services.AddBatching();    // Compile error - internal method
        }
    }
}
```

### Cloud-Native Configuration

```csharp
// Store configuration in environment variables
public class DatabaseSettings
{
    public string ConnectionString { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
    public bool EnableRetries { get; init; } = true;
}

// Register in Program.cs
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("Database"));
```

## Code Quality Standards

### .NET Analyzers Configuration

## 🚨 CRITICAL RULE: NEVER DISABLE ANALYZERS 🚨

**⚠️ ATTENTION: This rule is NON-NEGOTIABLE and applies to ALL code changes ⚠️**

**CRITICAL RULE**: Never disable analyzer rules to get around violations. Always fix the underlying issue.

### Why This Is Critical

- **Analyzers detect real problems** - They identify code quality issues, security vulnerabilities, and maintainability problems
- **Suppressing warnings hides problems** - It creates technical debt and makes code harder to maintain
- **Builds will fail** - Analyzer violations are treated as errors with `--warnaserror`
- **Code quality degrades** - When warnings are suppressed, code quality standards slip
- **Team productivity suffers** - Technical debt from suppressed warnings slows down development

### The Rule: Fix, Don't Suppress

- **ALWAYS fix the underlying issue** - Improve your code to satisfy the analyzer
- **NEVER use `#pragma warning disable`** without explicit approval and exhaustive justification
- **NEVER add `[SuppressMessage]` attributes** to hide violations
- **NEVER disable analyzer rules** in project files or configuration
- **NEVER ignore analyzer warnings** - They indicate real problems that need attention

#### JetBrains Rider .DotSettings Configuration

This project uses JetBrains Rider `.DotSettings` files for analyzer configuration. These files are committed to the repository and ensure consistent analysis across all development environments.

#### Analyzer Rule Enforcement Policy

- **Always fix the violation first** - The primary approach is to resolve the analyzer warning by improving the code
- **Never disable rules project-wide** - Disabling entire categories or rules across the project is prohibited
- **No shortcuts allowed** - Taking shortcuts by suppressing warnings undermines code quality
- **Exhaustive problem-solving required** - Must attempt all possible solutions before considering suppressions

#### Last Resort Suppression Guidelines

**Only when ALL other options are exhausted** and you can confirm there is absolutely no way to fix the violation:

1. **Single-line suppression only** - Use `#pragma warning disable` for the specific line, never for entire files or projects
2. **Explicit user confirmation required** - Must get explicit approval from code reviewer or team lead
3. **Mandatory justification** - Must document why the suppression is necessary and what was attempted
4. **Restore immediately** - Use `#pragma warning restore` on the very next line

```csharp
// ❌ NEVER DO THIS - Project-wide or file-wide suppressions
#pragma warning disable CA1234 // At file level
[assembly: SuppressMessage("Category", "Rule")] // At assembly level

// ✅ LAST RESORT ONLY - Single line with justification
public void SpecialCase()
{
    // Justification: Legacy interop requirement with unmanaged code that cannot be refactored
    // due to external system constraints. Approved by: [Name] on [Date]
    // Attempted solutions: Wrapper classes, safe handles, async patterns - all incompatible
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
    [System.Runtime.InteropServices.DllImport("legacy.dll")] 
    static extern void LegacyCall(string data);
#pragma warning restore CA2101
    
    LegacyCall("data");
}
```

#### Required Documentation for Suppressions

Any suppression must include:

- **Why**: Detailed explanation of why the code cannot be fixed
- **What was tried**: List of all attempted solutions
- **Who approved**: Name of reviewer who confirmed necessity
- **When**: Date of approval
- **Review date**: When this should be revisited

#### Build Pipeline Integration

- All analyzer violations are treated as build errors (aligns with `--warnaserror` policy)
- Suppressions trigger additional code review requirements
- Automated checks verify suppressions include proper justification
- Regular audits of suppressions ensure they remain necessary

### Unit Testing Requirements

```csharp
[Fact]
public async Task ProcessOrder_ValidOrder_ReturnsSuccess()
{
    // Arrange
    var mockRepository = new Mock<IOrderRepository>();
    var mockValidator = new Mock<IOrderValidator>();
    mockValidator.Setup(v => v.Validate(It.IsAny<Order>()))
             .Returns(ValidationResult.Success);
    
    var processor = new OrderProcessor(mockRepository.Object, mockValidator.Object);
    var order = new Order("123", "Test Product", 100m);
    
    // Act
    var result = await processor.ProcessAsync(order);
    
    // Assert
    Assert.True(result.IsSuccess);
    mockRepository.Verify(r => r.SaveAsync(order), Times.Once);
}
```

## Anti-Patterns to Avoid

### Violation of Single Responsibility

```csharp
// ❌ BAD - Class doing too many things
public class OrderManager
{
    public void ValidateOrder(Order order) { }
    public void SaveOrder(Order order) { }
    public void SendEmail(Order order) { }
    public void UpdateInventory(Order order) { }
    public void GenerateInvoice(Order order) { }
}
```

### Blocking Calls in Async Context

```csharp
// ❌ BAD - Blocking async calls
public async Task ProcessDataAsync()
{
    var data = await GetDataAsync();
    var result = ProcessData(data).Result; // Deadlock risk!
}

// ✅ GOOD - Proper async/await
public async Task ProcessDataAsync()
{
    var data = await GetDataAsync();
    var result = await ProcessDataInternalAsync(data);
}
```

### Mutable Shared State

```csharp
// ❌ BAD - Mutable shared state
public class Counter
{
    public static int Value = 0; // Shared mutable state
    
    public void Increment() => Value++; // Race conditions
}

// ✅ GOOD - Immutable or properly isolated state
public class Counter
{
    private int _value; // Private field for internal state is acceptable
    private object Lock { get; } = new(); // Lock object as property
    
    public int Increment()
    {
        lock (Lock)
        {
            return ++_value;
        }
    }
}
```

### Access Control Anti-Patterns

```csharp
// ❌ BAD - Overly exposed class without justification
public class InternalDataProcessor  // Should be internal
{
    public string _rawData;  // Should be private
    public void InternalMethod() { } // Should be private
}

// ❌ BAD - Unnecessary inheritance enabled
public class BaseEventHandler  // Should be sealed unless inheritance needed
{
    protected virtual void ProcessEvent() { } // Creates coupling
}

// ❌ BAD - Interface exposed unnecessarily
public interface IInternalHelper  // Should be internal
{
    void HelperMethod();
}

// ❌ BAD - Violates Orleans POCO pattern
public class OrderGrain : Grain, IOrderGrain  // Should use IGrainBase
{
    // Wrong: Inheriting from Grain instead of using POCO pattern
}

// ✅ GOOD - Proper access control
namespace Mississippi.EventSourcing.Internal
{
    /// <summary>
    /// Internal data processor for stream validation.
    /// Sealed to prevent inheritance, internal to limit exposure.
    /// </summary>
    internal sealed class DataProcessor
    {
        private string rawData;  // Private field
        private ILogger<DataProcessor> Logger { get; }  // Following DI pattern
        
        public DataProcessor(ILogger<DataProcessor> logger)
        {
            Logger = logger;
        }
        
        // Only expose what's needed publicly
        public ProcessingResult Process(string input)
        {
            rawData = input;
            return ProcessInternal();
        }
        
        // Keep implementation details private
        private ProcessingResult ProcessInternal()
        {
            Logger.DataProcessingStarted(rawData?.Length ?? 0);
            return new ProcessingResult();
        }
    }
}

// ✅ GOOD - Orleans POCO pattern with proper access control
namespace Mississippi.EventSourcing.Grains
{
    /// <summary>
    /// Order processing grain using POCO pattern.
    /// Sealed to prevent inheritance, public as it's a grain interface.
    /// </summary>
    public sealed class OrderGrain : IGrainBase, IOrderGrain
    {
        public IGrainContext GrainContext { get; }
        private ILogger<OrderGrain> Logger { get; }
        
        public OrderGrain(IGrainContext grainContext, ILogger<OrderGrain> logger)
        {
            GrainContext = grainContext;
            Logger = logger;
        }
    }
}
```

## Enforcement

These practices should be enforced through:

1. **Code Reviews**: Always verify adherence to SOLID principles and access control decisions
2. **Static Analysis**: Use .NET analyzers and treat warnings as errors, especially for access modifier violations
3. **Unit Tests**: Maintain high test coverage and quality
4. **Build Pipeline**: Fail builds on quality gate violations, including improper access control
5. **Documentation**: Keep these guidelines updated with examples

## Related Guidelines

This document should be read in conjunction with:

- **Service Registration and Configuration** (`.github/instructions/service-registration.instructions.md`) - For hierarchical service registration patterns, Options pattern implementation, and configuration handling
- **Logging Rules** (`.github/instructions/logging-rules.instructions.md`) - For high-performance logging patterns, LoggerExtensions classes, and mandatory ILogger usage with dependency injection properties
- **Orleans Best Practices** (`.github/instructions/orleans.instructions.md`) - For Orleans-specific grain development patterns, POCO grain requirements, and IGrainBase implementation with sealed classes
- **Orleans Serialization** (`.github/instructions/orleans-serialization.instructions.md`) - For Orleans serialization attributes and version tolerance patterns
- **Build Rules** (`.github/instructions/build-rules.instructions.md`) - For quality standards, zero warnings policy, and build pipeline requirements that enforce access control analyzer rules
- **Naming Conventions** (`.github/instructions/naming.instructions.md`) - For consistent naming patterns, feature-based namespace structure, and XML documentation requirements
- **Project File Management** (`.github/instructions/projects.instructions.md`) - For proper PackageReference usage and centralized package management

### Cross-Reference Alignment

- **Access Control + Orleans**: All grain classes must be `sealed` and implement `IGrainBase` following POCO pattern
- **Access Control + Logging**: All LoggerExtensions classes must be `public static` with properly scoped methods following access control principles
- **Access Control + Service Registration**: All ServiceRegistration classes must follow access control principles with proper public/internal boundaries
- **Access Control + Build Rules**: Access modifier violations must be fixed through code changes, not analyzer suppressions
- **C# + Service Registration**: Service registration must follow dependency injection property pattern and access control principles defined in this document
- **C# + Logging**: All services must use dependency injection property pattern for ILogger injection as specified in both documents

## Further Reading

- SOLID principles in C#: [https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/may/csharp-best-practices-dangers-of-violating-solid-principles-in-csharp](https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/may/csharp-best-practices-dangers-of-violating-solid-principles-in-csharp)
- Unit-testing best practices: [https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- NuGet & first-party packages: [https://learn.microsoft.com/en-us/nuget/what-is-nuget](https://learn.microsoft.com/en-us/nuget/what-is-nuget)
- DRY principle: [https://en.wikipedia.org/wiki/Don%27t_repeat_yourself](https://en.wikipedia.org/wiki/Don%27t_repeat_yourself)
- KISS principle: [https://en.wikipedia.org/wiki/KISS_principle](https://en.wikipedia.org/wiki/KISS_principle)
- Cloud-native definition (.NET): [https://learn.microsoft.com/en-us/dotnet/architecture/cloud-native/definition](https://learn.microsoft.com/en-us/dotnet/architecture/cloud-native/definition)
- C# records (immutability): [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
- Init-only setters: [https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init)
- Configure .NET code-analysis rules: [https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options)
- Orleans best practices: [https://learn.microsoft.com/en-us/dotnet/orleans/resources/best-practices](https://learn.microsoft.com/en-us/dotnet/orleans/resources/best-practices)
- Orleans external tasks & grains: [https://learn.microsoft.com/en-us/dotnet/orleans/grains/external-tasks-and-grains](https://learn.microsoft.com/en-us/dotnet/orleans/grains/external-tasks-and-grains)
- Twelve-Factor App manifesto: [https://www.12factor.net/](https://www.12factor.net/)
