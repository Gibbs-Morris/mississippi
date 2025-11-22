---
applyTo: '**/*.cs'
---

# Service Registration and Configuration Pattern

Governing thought: Use hierarchical feature-based registration with Options pattern and synchronous setup, deferring async initialization to hosted services or lifecycle participants.

## Rules (RFC 2119)

- Service registration classes **MUST** follow feature namespace structure.  
  Why: Maintains consistency and organization across the solution.
- Each feature **MUST** have `public static class ServiceRegistration` with `Add{FeatureName}()` methods.  
  Why: Provides consistent registration pattern across features.
- Parent features **MUST** call child feature registrations to build composable service collections.  
  Why: Enables hierarchical composition and modular design.
- Registration methods **MUST** be `public` only at product/feature boundaries.  
  Why: Clarifies intended usage and prevents misuse of internal components.
- Sub-features and implementation components **MUST** have `internal` registration methods.  
  Why: Hides implementation details and reduces API surface area.
- Direct configuration parameters **MUST NOT** be used in constructors; **MUST** use `IOptions<T>`, `IOptionsSnapshot<T>`, or `IOptionsMonitor<T>`.  
  Why: Centralizes configuration, supports validation, and enables hot-reload.
- Multiple configuration overloads **MUST** be provided for `Action<TOptions>`, `IConfiguration`, and explicit parameters.  
  Why: Supports different consumption patterns and flexibility.
- Core service registration **MUST** be implemented in private parameterless method.  
  Why: Separates service registration from configuration handling for clarity.
- Options classes **MUST** follow `{FeatureName}Options` naming pattern.  
  Why: Maintains naming consistency across configuration classes.
- Options classes **MUST** provide sensible defaults using property initializers.  
  Why: Enables "convention over configuration" approach.
- Options **MUST** be validated at startup using `IValidateOptions<T>` or `ValidateOnStart()`.  
  Why: Catches configuration errors early before runtime failures.
- Registered services **MUST** use `private Type Name { get; }` pattern when injected.  
  Why: Follows dependency injection property pattern consistently.
- Registration methods **MUST** use `Add{FeatureName}()` naming convention.  
  Why: Maintains consistency with .NET extension method patterns.
- Public registration methods **MUST** include XML documentation with parameter and return value documentation.  
  Why: Provides IntelliSense and API documentation for consumers.
- Service registration **MUST** be synchronous; **MUST NOT** perform async operations.  
  Why: DI container building is synchronous; async operations cause blocking and deadlocks.
- Async setup operations **MUST** be deferred to IHostedService implementations.  
  Why: Hosted services run after DI container is built and support async initialization.
- Orleans-specific initialization **MUST** use Orleans lifecycle participants.  
  Why: Enables initialization at specific Orleans lifecycle stages.
- Factories performing async operations **MUST** be registered for deferred initialization.  
  Why: Avoids blocking during registration while enabling async work when needed.
- Database and external service initialization **MUST** use hosted services or lifecycle participants.  
  Why: Prevents blocking during DI container building and startup.
- Connection strings **MUST** be accepted as parameters and register clients using factory patterns.  
  Why: Avoids direct instantiation in DI and enables testability.
- Registration classes **SHOULD** be sealed and follow minimal access principles.  
  Why: Aligns with C# access control guidelines.
- Configuration **SHOULD** be externalized following cloud-native principles.  
  Why: Enables environment-specific configuration without code changes.

## Scope and Audience

**Audience:** Developers implementing service registration and DI configuration in the Mississippi Framework.

**In scope:** Hierarchical registration, Options pattern, configuration overloads, async initialization patterns, IHostedService, Orleans lifecycle participants, factory patterns.

**Out of scope:** Specific service implementations, business logic, Orleans grain implementation details.

## Purpose

This document defines service registration standards ensuring consistency, maintainability, and proper configuration handling across features through hierarchical feature-based registration and Options pattern.

## Core Principles

- Feature-aligned organization following namespace structure
- ServiceRegistration class per feature with Add methods
- Parent-child registration pattern for composable service collections
- Public at logical boundaries, internal for implementation details
- Always use Options pattern (never direct configuration parameters)
- Make everything configurable following cloud-native principles
- Support multiple configuration overloads (Action, IConfiguration, explicit)
- Private core registration method + public configuration overloads
- Use naming pattern: {FeatureName}Options
- Provide sensible defaults in options classes
- Validate options at startup
- Accept connection strings as parameters, use factory patterns
- Follow dependency injection property pattern (private Type Name { get; })
- Use Add{FeatureName}() naming convention
- Include XML documentation on public methods
- Service registration must be synchronous (no async operations)
- Use IHostedService for async initialization
- Use Orleans lifecycle participants for Orleans-specific init
- Register factories for deferred async operations
- Never initialize database/external services inline during registration

## Service Registration Implementation Pattern

### Required File Structure

```text
Mississippi.EventSourcing/
├── ServiceRegistration.cs              // Calls child registrations
├── Streams/
│   ├── ServiceRegistration.cs          // Registers stream services
│   └── Batching/
│       └── ServiceRegistration.cs      // Registers batching services
└── Storage/
    └── ServiceRegistration.cs          // Registers storage services
```

### Private Core + Public Overloads Pattern

```csharp
namespace Mississippi.EventSourcing.Cosmos
{
    /// <summary>
    /// Provides service registration for Cosmos DB storage with comprehensive configuration support.
    /// Follows the established pattern of private core method + public configuration overloads.
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Private core registration method that registers all services without configuration.
        /// This is called by all public overloads after configuration is handled.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        /// <returns>The service collection for method chaining.</returns>
        private static IServiceCollection AddCosmosBrookStorageProvider(this IServiceCollection services)
        {
            // Register all core services using dependency injection property pattern
            return services
                .AddSingleton<IBrookStorageProvider, BrookStorageProvider>()
                .AddSingleton<IBrookRecoveryService, BrookRecoveryService>()
                .AddSingleton<IEventBrookReader, EventBrookReader>()
                .AddSingleton<IEventBrookAppender, EventBrookAppender>()
                .AddSingleton<ICosmosRepository, CosmosRepository>()
                .AddSingleton<IDistributedLockManager, BlobDistributedLockManager>()
                .AddSingleton<IBatchSizeEstimator, BatchSizeEstimator>()
                .AddSingleton<IRetryPolicy, CosmosRetryPolicy>()
                .AddSingleton<CosmosContainerInitializationService>()
                .AddHostedService<CosmosContainerInitializationService>(provider =>
                    provider.GetRequiredService<CosmosContainerInitializationService>())
                .AddSingleton<Container>(provider =>
                {
                    var initService = provider.GetRequiredService<CosmosContainerInitializationService>();
                    return initService.GetContainer();
                });
        }

        /// <summary>
        /// Registers Cosmos DB storage provider with explicit connection strings and optional configuration.
        /// This overload supports the most common usage pattern with connection strings.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        /// <param name="cosmosConnectionString">The Cosmos DB connection string.</param>
        /// <param name="blobStorageConnectionString">The Azure Blob Storage connection string for distributed locking.</param>
        /// <param name="configureOptions">Optional action to configure additional storage options.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddCosmosBrookStorageProvider(
            this IServiceCollection services,
            string cosmosConnectionString,
            string blobStorageConnectionString,
            Action<BrookStorageOptions>? configureOptions = null)
        {
            // Register clients using factory pattern (not direct instantiation)
            services.AddSingleton<CosmosClient>(_ => new CosmosClient(cosmosConnectionString));
            services.AddSingleton<BlobServiceClient>(_ => new BlobServiceClient(blobStorageConnectionString));

            // Configure options if provided
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            return services.AddCosmosBrookStorageProvider();
        }

        /// <summary>
        /// Registers Cosmos DB storage provider with configuration action only.
        /// This overload is useful when connection strings are configured elsewhere.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        /// <param name="configureOptions">Action to configure the storage options.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddCosmosBrookStorageProvider(
            this IServiceCollection services,
            Action<BrookStorageOptions> configureOptions)
        {
            services.Configure(configureOptions);
            return services.AddCosmosBrookStorageProvider();
        }

        /// <summary>
        /// Registers Cosmos DB storage provider with IConfiguration binding.
        /// This overload supports appsettings.json and other configuration sources.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        /// <param name="configuration">The configuration section containing BrookStorageOptions.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddCosmosBrookStorageProvider(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<BrookStorageOptions>(configuration);
            return services.AddCosmosBrookStorageProvider();
        }

        /// <summary>
        /// Registers Cosmos DB storage provider with validation.
        /// This overload demonstrates options validation for production scenarios.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        /// <param name="cosmosConnectionString">The Cosmos DB connection string.</param>
        /// <param name="blobStorageConnectionString">The Azure Blob Storage connection string.</param>
        /// <param name="configureOptions">Action to configure the storage options.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddCosmosBrookStorageProviderWithValidation(
            this IServiceCollection services,
            string cosmosConnectionString,
            string blobStorageConnectionString,
            Action<BrookStorageOptions>? configureOptions = null)
        {
            // Register clients
            services.AddSingleton<CosmosClient>(_ => new CosmosClient(cosmosConnectionString));
            services.AddSingleton<BlobServiceClient>(_ => new BlobServiceClient(blobStorageConnectionString));

            // Configure options with validation
            var optionsBuilder = services.AddOptions<BrookStorageOptions>();
            
            if (configureOptions != null)
            {
                optionsBuilder.Configure(configureOptions);
            }
            
            // Add validation rules
            optionsBuilder.Validate(options =>
            {
                return !string.IsNullOrWhiteSpace(options.DatabaseName) &&
                       !string.IsNullOrWhiteSpace(options.EventsContainer) &&
                       !string.IsNullOrWhiteSpace(options.HeadsContainer) &&
                       options.MaxBatchSize > 0 &&
                       options.RequestTimeoutSeconds > 0 &&
                       options.MaxRetryAttempts >= 0;
            }, "BrookStorageOptions validation failed. Check DatabaseName, container names, and numeric values.")
            .ValidateOnStart(); // Fail fast during startup if configuration is invalid

            return services.AddCosmosBrookStorageProvider();
        }
    }
}
```

### Options Class Pattern

```csharp
namespace Mississippi.EventSourcing.Cosmos
{
    /// <summary>
    /// Configuration options for Cosmos DB brook storage provider.
    /// Provides sensible defaults while allowing full customization.
    /// </summary>
    public sealed class BrookStorageOptions
    {
        /// <summary>
        /// Gets or sets the Cosmos DB database name.
        /// Default: "EventStore"
        /// </summary>
        [Required]
        [MinLength(1)]
        public string DatabaseName { get; set; } = "EventStore";
        
        /// <summary>
        /// Gets or sets the events container name.
        /// Default: "Events"
        /// </summary>
        [Required]
        [MinLength(1)]
        public string EventsContainer { get; set; } = "Events";
        
        /// <summary>
        /// Gets or sets the heads container name.
        /// Default: "Heads"
        /// </summary>
        [Required]
        [MinLength(1)]
        public string HeadsContainer { get; set; } = "Heads";
        
        /// <summary>
        /// Gets or sets the maximum batch size for event operations.
        /// Default: 100
        /// </summary>
        [Range(1, 10000)]
        public int MaxBatchSize { get; set; } = 100;
        
        /// <summary>
        /// Gets or sets the request timeout in seconds.
        /// Default: 30
        /// </summary>
        [Range(1, 300)]
        public int RequestTimeoutSeconds { get; set; } = 30;
        
        /// <summary>
        /// Gets or sets the maximum retry attempts for transient failures.
        /// Default: 3
        /// </summary>
        [Range(0, 10)]
        public int MaxRetryAttempts { get; set; } = 3;
        
        /// <summary>
        /// Gets or sets a value indicating whether to enable automatic container creation.
        /// Default: true
        /// </summary>
        public bool AutoCreateContainers { get; set; } = true;
    }
}
```

### Hierarchical Registration Example

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
```

## Async Initialization Patterns

### IHostedService for Database Initialization

```csharp
namespace Mississippi.EventSourcing.Cosmos
{
    /// <summary>
    /// Hosted service for initializing Cosmos DB containers during application startup.
    /// Demonstrates proper async initialization outside of service registration.
    /// </summary>
    internal sealed class CosmosContainerInitializationService : IHostedService
    {
        private ILogger<CosmosContainerInitializationService> Logger { get; }
        private IOptions<BrookStorageOptions> Options { get; }
        private CosmosClient CosmosClient { get; }
        private Container? _container;

        public CosmosContainerInitializationService(
            ILogger<CosmosContainerInitializationService> logger,
            IOptions<BrookStorageOptions> options,
            CosmosClient cosmosClient)
        {
            Logger = logger;
            Options = options;
            CosmosClient = cosmosClient;
        }

        /// <summary>
        /// Initializes Cosmos DB database and containers asynchronously during startup.
        /// This runs after DI container is built, allowing async operations.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logger.CosmosInitializationStarted(Options.Value.DatabaseName);

                // Create database if it doesn't exist
                var databaseResponse = await CosmosClient.CreateDatabaseIfNotExistsAsync(
                    Options.Value.DatabaseName,
                    cancellationToken: cancellationToken);

                var database = databaseResponse.Database;

                // Create containers if auto-creation is enabled
                if (Options.Value.AutoCreateContainers)
                {
                    // Create events container
                    await database.CreateContainerIfNotExistsAsync(
                        Options.Value.EventsContainer,
                        "/partitionKey",
                        cancellationToken: cancellationToken);

                    // Create heads container
                    await database.CreateContainerIfNotExistsAsync(
                        Options.Value.HeadsContainer,
                        "/partitionKey",
                        cancellationToken: cancellationToken);
                }

                // Store container reference for factory
                _container = database.GetContainer(Options.Value.EventsContainer);

                Logger.CosmosInitializationCompleted(Options.Value.DatabaseName);
            }
            catch (Exception ex)
            {
                Logger.CosmosInitializationFailed(Options.Value.DatabaseName, ex);
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.CosmosShutdownStarted();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the initialized container. Throws if initialization hasn't completed.
        /// </summary>
        public Container GetContainer()
        {
            return _container ?? throw new InvalidOperationException(
                "Container not initialized. Ensure CosmosContainerInitializationService has started.");
        }
    }

    /// <summary>
    /// Service registration that properly uses hosted service for async initialization.
    /// Registration remains synchronous, async work deferred to hosted service.
    /// </summary>
    public static class ServiceRegistration
    {
        public static IServiceCollection AddCosmosBrookStorageProvider(
            this IServiceCollection services,
            string cosmosConnectionString,
            string blobStorageConnectionString,
            Action<BrookStorageOptions>? configureOptions = null)
        {
            // ✅ GOOD: Synchronous client registration
            services.AddSingleton<CosmosClient>(_ => new CosmosClient(cosmosConnectionString));
            services.AddSingleton<BlobServiceClient>(_ => new BlobServiceClient(blobStorageConnectionString));

            // ✅ GOOD: Register hosted service for async initialization
            services.AddSingleton<CosmosContainerInitializationService>();
            services.AddHostedService<CosmosContainerInitializationService>(provider =>
                provider.GetRequiredService<CosmosContainerInitializationService>());

            // ✅ GOOD: Factory pattern for deferred container access
            services.AddSingleton<Container>(provider =>
            {
                var initService = provider.GetRequiredService<CosmosContainerInitializationService>();
                return initService.GetContainer(); // Will throw if not initialized
            });

            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            return services.AddCosmosBrookStorageProvider();
        }
    }
}
```

### Orleans Lifecycle Participant Pattern

```csharp
namespace Mississippi.EventSourcing.Orleans
{
    /// <summary>
    /// Orleans lifecycle participant for event sourcing initialization.
    /// Demonstrates proper Orleans-specific async initialization.
    /// </summary>
    internal sealed class EventSourcingLifecycleParticipant : ILifecycleParticipant<ISiloLifecycle>
    {
        private ILogger<EventSourcingLifecycleParticipant> Logger { get; }
        private IStreamProvider StreamProvider { get; }

        public EventSourcingLifecycleParticipant(
            ILogger<EventSourcingLifecycleParticipant> logger,
            IStreamProvider streamProvider)
        {
            Logger = logger;
            StreamProvider = streamProvider;
        }

        public void Participate(ISiloLifecycle lifecycle)
        {
            // Register for specific Orleans lifecycle stages
            lifecycle.Subscribe(
                nameof(EventSourcingLifecycleParticipant),
                ServiceLifecycleStage.ApplicationServices,
                OnStart,
                OnStop);
        }

        private async Task OnStart(CancellationToken cancellationToken)
        {
            Logger.EventSourcingInitializationStarted();

            try
            {
                // Perform Orleans-specific async initialization
                await InitializeStreamProvidersAsync(cancellationToken);
                await ValidateGrainConfigurationAsync(cancellationToken);

                Logger.EventSourcingInitializationCompleted();
            }
            catch (Exception ex)
            {
                Logger.EventSourcingInitializationFailed(ex);
                throw;
            }
        }

        private async Task OnStop(CancellationToken cancellationToken)
        {
            Logger.EventSourcingShutdownStarted();
            
            // Cleanup async resources
            await CleanupStreamResourcesAsync(cancellationToken);
            
            Logger.EventSourcingShutdownCompleted();
        }

        private async Task InitializeStreamProvidersAsync(CancellationToken cancellationToken)
        {
            // Orleans-specific async initialization
            var streams = StreamProvider.GetStreamsAsync<string>("EventStream");
            await foreach (var stream in streams.WithCancellation(cancellationToken))
            {
                // Initialize stream subscriptions
            }
        }

        private Task ValidateGrainConfigurationAsync(CancellationToken cancellationToken)
        {
            // Validate Orleans grain configuration
            return Task.CompletedTask;
        }

        private Task CleanupStreamResourcesAsync(CancellationToken cancellationToken)
        {
            // Cleanup stream subscriptions
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Orleans silo configuration with lifecycle participant.
    /// </summary>
    public static class OrleansServiceRegistration
    {
        public static ISiloBuilder AddEventSourcing(this ISiloBuilder builder)
        {
            return builder
                .AddMemoryStreams("EventStreamProvider")
                .ConfigureServices(services =>
                {
                    // ✅ GOOD: Register lifecycle participant for Orleans-specific async work
                    services.AddSingleton<EventSourcingLifecycleParticipant>();
                    services.AddSingleton<ILifecycleParticipant<ISiloLifecycle>>(provider =>
                        provider.GetRequiredService<EventSourcingLifecycleParticipant>());
                });
        }
    }
}
```

### Factory Pattern for Deferred Async Operations

```csharp
namespace Mississippi.EventSourcing.Storage
{
    /// <summary>
    /// Factory interface for creating storage connections with async initialization.
    /// </summary>
    public interface IEventStoreConnectionFactory
    {
        Task<IEventStoreConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Factory implementation that performs async database connection setup.
    /// Defers async operations until first use, not during registration.
    /// </summary>
    internal sealed class EventStoreConnectionFactory : IEventStoreConnectionFactory
    {
        private ILogger<EventStoreConnectionFactory> Logger { get; }
        private IOptions<EventStoreOptions> Options { get; }
        private readonly SemaphoreSlim initializationLock = new(1, 1);
        private IEventStoreConnection? _connection;
        private bool _isInitialized;

        public EventStoreConnectionFactory(
            ILogger<EventStoreConnectionFactory> logger,
            IOptions<EventStoreOptions> options)
        {
            Logger = logger;
            Options = options;
        }

        public async Task<IEventStoreConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (_isInitialized && _connection != null)
            {
                return _connection;
            }

            await initializationLock.WaitAsync(cancellationToken);
            try
            {
                if (_isInitialized && _connection != null)
                {
                    return _connection;
                }

                Logger.EventStoreConnectionInitializationStarted();

                // ✅ GOOD: Async operations happen here, not during DI registration
                _connection = await CreateConnectionInternalAsync(cancellationToken);
                await _connection.ValidateConnectionAsync(cancellationToken);

                _isInitialized = true;

                Logger.EventStoreConnectionInitializationCompleted();
                return _connection;
            }
            catch (Exception ex)
            {
                Logger.EventStoreConnectionInitializationFailed(ex);
                throw;
            }
            finally
            {
                initializationLock.Release();
            }
        }

        private async Task<IEventStoreConnection> CreateConnectionInternalAsync(CancellationToken cancellationToken)
        {
            // Perform actual async database connection setup
            var connection = new EventStoreConnection(Options.Value.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }

    /// <summary>
    /// Service registration using factory pattern for deferred async operations.
    /// </summary>
    public static class ServiceRegistration
    {
        public static IServiceCollection AddEventStore(
            this IServiceCollection services,
            string connectionString,
            Action<EventStoreOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            // ✅ GOOD: Register factory for deferred async initialization
            services.AddSingleton<IEventStoreConnectionFactory, EventStoreConnectionFactory>();

            // ✅ GOOD: Register service that uses factory
            services.AddScoped<IEventStore>(provider =>
            {
                var factory = provider.GetRequiredService<IEventStoreConnectionFactory>();
                return new EventStore(factory, provider.GetRequiredService<ILogger<EventStore>>());
            });

            return services;
        }
    }
}
```

## Configuration Patterns

### appsettings.json Configuration

```json
{
  "BrookStorage": {
    "DatabaseName": "EventStore",
    "EventsContainer": "Events",
    "HeadsContainer": "Heads",
    "MaxBatchSize": 100,
    "RequestTimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "AutoCreateContainers": true
  },
  "ConnectionStrings": {
    "CosmosDb": "AccountEndpoint=https://...",
    "BlobStorage": "DefaultEndpointsProtocol=https;..."
  }
}
```

### Usage Patterns

```csharp
namespace SampleApplication
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            // Pattern 1: Connection strings with optional configuration
            services.AddCosmosBrookStorageProvider(
                "cosmos-connection-string",
                "blob-storage-connection-string",
                options =>
                {
                    options.DatabaseName = "MyEventStore";
                    options.MaxBatchSize = 200;
                    options.RequestTimeoutSeconds = 60;
                });

            // Pattern 2: Configuration action only (connection strings configured elsewhere)
            services.AddCosmosBrookStorageProvider(options =>
            {
                options.EventsContainer = "CustomEvents";
                options.HeadsContainer = "CustomHeads";
                options.AutoCreateContainers = false;
            });

            // Pattern 3: IConfiguration binding (appsettings.json)
            services.AddCosmosBrookStorageProvider(
                configuration.GetSection("BrookStorage"));

            // Pattern 4: With validation for production scenarios
            services.AddCosmosBrookStorageProviderWithValidation(
                "cosmos-connection-string",
                "blob-storage-connection-string",
                options =>
                {
                    options.DatabaseName = "ProductionEventStore";
                    options.MaxRetryAttempts = 5;
                });
        }
    }
}
```

## Anti-Patterns to Avoid

### Async Operations Anti-Patterns

```csharp
// ❌ BAD - Async operations during service registration
public static class BadAsyncServiceRegistration
{
    // NEVER DO THIS - Service registration must be synchronous
    public static async Task<IServiceCollection> AddEventSourcingBadAsync(this IServiceCollection services)
    {
        // ❌ WRONG: Async operations in registration method
        var cosmosClient = new CosmosClient("connection-string");
        await cosmosClient.CreateDatabaseIfNotExistsAsync("EventStore"); // Blocks DI setup!
        
        services.AddSingleton(cosmosClient);
        return services;
    }
    
    // NEVER DO THIS - Database calls during registration
    public static IServiceCollection AddEventSourcingWithDatabaseCheck(this IServiceCollection services)
    {
        // ❌ WRONG: Synchronous database calls that can block startup
        var connectionString = "connection-string";
        using var connection = new SqlConnection(connectionString);
        connection.Open(); // Can fail and blocks entire application startup!
        
        // Check if database exists
        var command = new SqlCommand("SELECT 1", connection);
        command.ExecuteScalar(); // Blocks DI container building!
        
        services.AddScoped<IEventStore>(_ => new EventStore(connectionString));
        return services;
    }
    
    // NEVER DO THIS - HTTP calls during registration
    public static IServiceCollection AddExternalServiceWithValidation(this IServiceCollection services)
    {
        // ❌ WRONG: HTTP calls during registration
        using var httpClient = new HttpClient();
        var response = httpClient.GetAsync("https://api.external.com/health").Result; // Blocks!
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("External service not available"); // Fails app startup
        }
        
        services.AddHttpClient<IExternalService, ExternalService>();
        return services;
    }
}

// ❌ BAD - Factory that performs async work synchronously
public static class BadSynchronousFactory
{
    public static IServiceCollection AddEventStore(this IServiceCollection services)
    {
        services.AddSingleton<IEventStore>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<EventStore>>();
            
            // ❌ WRONG: .Result or .Wait() on async operations
            var connection = CreateConnectionAsync().Result; // Deadlock risk!
            return new EventStore(connection, logger);
        });
        
        return services;
    }
    
    private static async Task<IDbConnection> CreateConnectionAsync()
    {
        var connection = new SqlConnection("connection-string");
        await connection.OpenAsync(); // This gets blocked by .Result above
        return connection;
    }
}

// ✅ GOOD - Proper patterns for async initialization
public static class GoodAsyncPatterns
{
    // ✅ GOOD: Register hosted service for async work
    public static IServiceCollection AddEventSourcing(this IServiceCollection services)
    {
        // Synchronous registration only
        services.AddSingleton<CosmosClient>(_ => new CosmosClient("connection-string"));
        
        // Async work delegated to hosted service
        services.AddSingleton<DatabaseInitializationService>();
        services.AddHostedService<DatabaseInitializationService>(provider =>
            provider.GetRequiredService<DatabaseInitializationService>());
        
        // Factory for deferred async operations
        services.AddScoped<IEventStore>(provider =>
        {
            var factory = provider.GetRequiredService<IEventStoreConnectionFactory>();
            return new EventStore(factory, provider.GetRequiredService<ILogger<EventStore>>());
        });
        
        return services;
    }
    
    // ✅ GOOD: Orleans lifecycle participant for Orleans-specific async work
    public static ISiloBuilder AddEventSourcing(this ISiloBuilder builder)
    {
        return builder.ConfigureServices(services =>
        {
            // Synchronous registration
            services.AddSingleton<EventSourcingLifecycleParticipant>();
            services.AddSingleton<ILifecycleParticipant<ISiloLifecycle>>(provider =>
                provider.GetRequiredService<EventSourcingLifecycleParticipant>());
        });
    }
    
    // ✅ GOOD: Configuration validation without async operations
    public static IServiceCollection AddEventSourcingWithValidation(this IServiceCollection services)
    {
        services.AddOptions<EventSourcingOptions>()
            .Validate(options => !string.IsNullOrEmpty(options.ConnectionString))
            .ValidateOnStart(); // Validates synchronously at startup
        
        return services;
    }
}
```

### Service Registration Anti-Patterns

```csharp
// ❌ BAD - Monolithic registration file
public static class AllServiceRegistrations  // Violates feature separation
{
    public static IServiceCollection AddEverything(this IServiceCollection services)
    {
        // 200+ lines of unrelated service registrations
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEventStore, EventStore>();
        services.AddScoped<IBatchProcessor, BatchProcessor>();
        // ... many more unrelated services
        return services;
    }
}

// ❌ BAD - Wrong access levels
public static class InternalServiceRegistration
{
    // Wrong: Implementation detail exposed as public
    public static IServiceCollection AddBatching(this IServiceCollection services)
    {
        return services.AddScoped<IBatchProcessor, BatchProcessor>();
    }
}

// ❌ BAD - Non-hierarchical registration
public static class EventSourcingRegistration
{
    public static IServiceCollection AddEventSourcing(this IServiceCollection services)
    {
        // Wrong: Manually registering all sub-components instead of calling child registrations
        services.AddScoped<IStreamProcessor, StreamProcessor>();
        services.AddScoped<IBatchProcessor, BatchProcessor>();
        services.AddScoped<IEventStore, EventStore>();
        return services;
    }
}

// ❌ BAD - Wrong naming convention
public static class EventSourcingServices  // Should be "ServiceRegistration"
{
    public static IServiceCollection RegisterEventSourcing(...)  // Should be "AddEventSourcing"
    {
        return services;
    }
}
```

### Configuration Anti-Patterns

```csharp
// ❌ BAD - Direct configuration parameters in constructors
public class BadEventStore
{
    public BadEventStore(
        string cosmosConnectionString,    // Should use IOptions<T>
        string databaseName,              // Should use IOptions<T>  
        int maxRetryAttempts,            // Should use IOptions<T>
        bool autoCreateContainers)       // Should use IOptions<T>
    {
        // Wrong: Direct parameter injection instead of Options pattern
    }
}

// ❌ BAD - Hard-coded configuration values
public class BadStorageProvider
{
    private const string DatabaseName = "EventStore";        // Should be configurable
    private const int MaxBatchSize = 100;                   // Should be configurable
    private const string ConnectionString = "hardcoded";     // Major security issue
}

// ❌ BAD - Wrong Options pattern usage
public class BadOptionsConsumer
{
    private readonly MyOptions _options; // Should be IOptions<MyOptions>
    
    public BadOptionsConsumer(MyOptions options) // Wrong: Direct options injection
    {
        _options = options;
    }
}

// ✅ GOOD - Proper Options pattern with dependency injection properties
internal sealed class EventStore : IEventStore
{
    // Following mandatory dependency injection property pattern
    private ILogger<EventStore> Logger { get; }
    private IOptions<EventStoreOptions> Options { get; }
    private CosmosClient CosmosClient { get; }
    
    public EventStore(
        ILogger<EventStore> logger,
        IOptions<EventStoreOptions> options,    // Correct: IOptions<T> injection
        CosmosClient cosmosClient)
    {
        Logger = logger;
        Options = options;
        CosmosClient = cosmosClient;
    }
    
    public async Task StoreEventAsync(DomainEvent domainEvent)
    {
        // Access configuration through Options.Value
        var database = CosmosClient.GetDatabase(Options.Value.DatabaseName);
        var batchSize = Options.Value.MaxBatchSize;
        
        Logger.EventStoreOperationStarted(domainEvent.Id, batchSize);
        // Implementation using configured values
    }
}
```

## Enforcement

These service registration standards should be enforced through:

1. **Code Reviews**: Always verify adherence to hierarchical registration patterns, access control decisions, configuration handling, and synchronous registration requirements
2. **Static Analysis**: Use .NET analyzers and treat warnings as errors, especially for access modifier violations and async operation detection in registration methods
3. **Unit Tests**: Maintain high test coverage and quality for service registration methods, configuration validation, and hosted service initialization
4. **Build Pipeline**: Fail builds on quality gate violations, including improper access control, missing configuration support, and async operations in registration
5. **Documentation**: Keep these guidelines updated with examples, especially for async initialization patterns and Orleans lifecycle participation
6. **Configuration Validation**: Ensure all Options classes include validation and fail-fast startup checks
7. **Service Registration Reviews**: Verify all registration methods are synchronous and follow the private core + public overloads pattern with proper configuration support
8. **Async Pattern Reviews**: Ensure all async initialization uses IHostedService, Orleans lifecycle participants, or factory patterns - never inline in registration methods
9. **Orleans Integration Reviews**: Verify Orleans-specific async work uses lifecycle participants and proper service lifecycle stages

---
Last verified: 2025-11-09
Default branch: main

## Further Reading

- .NET dependency injection: [https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- Options pattern in .NET: [https://learn.microsoft.com/en-us/dotnet/core/extensions/options](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)
- Configuration in .NET: [https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
- Extension methods: [https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods)
- IHostedService background tasks: [https://learn.microsoft.com/en-us/dotnet/core/extensions/hosted-services](https://learn.microsoft.com/en-us/dotnet/core/extensions/hosted-services)
- Orleans lifecycle management: [https://learn.microsoft.com/en-us/dotnet/orleans/implementation/grain-lifecycle](https://learn.microsoft.com/en-us/dotnet/orleans/implementation/grain-lifecycle)
- Orleans startup tasks: [https://learn.microsoft.com/en-us/dotnet/orleans/implementation/startup-tasks](https://learn.microsoft.com/en-us/dotnet/orleans/implementation/startup-tasks)
- Factory pattern in .NET: [https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#factory-pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#factory-pattern)
