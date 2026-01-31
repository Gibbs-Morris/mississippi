# Mississippi Service Registration Design

> **Status:** Design Document (Source of Truth for Registration Redesign)  
> **Goal:** Best-in-class developer experience with zero-config defaults and full escape hatches  
> **Implementation:** See [Registration Implementation Approach](./registration-implementation.md)

---

## Design Principles

1. **3-Line Host Setup** - Most apps need only: Mississippi → Domain → Providers
2. **Zero-Config Defaults** - Sensible defaults live in Options classes; explicit config is opt-in
3. **Idempotent Registration** - All infrastructure uses `TryAdd*`; safe to call multiple times
4. **Generated Composites** - One `[GenerateMississippiApp]` attribute generates all host composites
5. **Escape Hatches** - Every layer is decomposable; power users can override anything
6. **Provider Pluggability** - Storage providers are explicitly separate and swappable

---

## Current State Analysis

### What Exists on This Branch

The `feature/inlet-client-composite-generator` branch has made significant progress:

| Component | Status | Location |
|-----------|--------|----------|
| `InletClientCompositeGenerator` | ✅ Implemented | `src/Inlet.Client.Generators/` |
| `[GenerateInletClientComposite]` attribute | ✅ Implemented | `src/Inlet.Generators.Abstractions/` |
| Spring.Client composite | ✅ Using generated `AddSpringInlet()` | `samples/Spring/Spring.Client/Program.cs` |
| Spring.Server composite | 🔨 Manual (marked `[PendingSourceGenerator]`) | `samples/Spring/Spring.Server/Infrastructure/` |
| Spring.Silo composite | 🔨 Manual (marked `[PendingSourceGenerator]`) | `samples/Spring/Spring.Silo/Infrastructure/` |
| Layered registration files | ✅ Extracted (Server: 5 files, Silo: 6 files) | `Infrastructure/` folders |

### Program.cs Target State (Achieved)

**Spring.Client** (3 lines - using generated composite):

```csharp
builder.Services.AddSpringInlet();  // Generated from [assembly: GenerateInletClientComposite]
builder.Services.AddEntitySelectionFeature();  // App-specific, not generated
await builder.Build().RunAsync();
```

**Spring.Server** (4 lines - using manual composite):

```csharp
builder.AddSpringServer();
WebApplication app = builder.Build();
app.UseSpringServer();
await app.RunAsync();
```

**Spring.Silo** (4 lines - using manual composite):

```csharp
builder.AddSpringSilo();
WebApplication app = builder.Build();
app.UseSpringSilo();
await app.RunAsync();
```

### Issues Identified

| # | Issue | Evidence | Impact |
|---|-------|----------|--------|
| 1 | **Duplicated Infrastructure** | `AddAggregateSupport()` called 5+ times across Spring | Confusion, potential ordering bugs |
| 2 | **Stream Provider Sprawl** | `StreamProviderName` appears in 9+ separate locations | Typo risk, maintenance burden |
| 2a | **God Constants Class** | `MississippiDefaults` couples everyone to one type | Breaking changes cascade, mixed concerns |
| 3 | **Hidden Ordering Dependencies** | "AddInletSilo must be called before ScanProjectionAssemblies" | Silent failures, debugging nightmares |
| 4 | **Inconsistent Extension Split** | Some methods on `IServiceCollection`, others on `IHostBuilder` | Cognitive load |
| 5 | **Missing Server/Silo Generators** | Only client has `InletClientCompositeGenerator` | Manual boilerplate for server/silo |
| 6 | **Scattered Domain Registration** | Domain setup split across 4+ registration files | Hard to understand app topology |
| 7 | **Verbose Provider Config** | Cosmos requires 3+ builder calls with repeated connection strings | Error-prone duplication |

### What `MississippiDefaults` Contains Today

```csharp
public static class MississippiDefaults
{
    public const string DatabaseId = "mississippi";
    public const string StreamProviderName = "mississippi-streaming";
    
    public static class ContainerIds { Brooks, Locks, Snapshots }
    public static class ServiceKeys { BlobLocking, CosmosBrooks, CosmosBrooksClient, ... }
    public static class StreamNamespaces { AllClients, Server }
}
```

**Problem:** This is a "god constants" class. Everyone depends on it. Changing any default is a breaking change for everyone. It mixes concerns (Cosmos, Orleans streams, SignalR namespaces).

### Current Sample Code (Spring.Silo)

```csharp
// Today: 15+ lines, ordering-sensitive, repetitive
var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddSpringSilo();  // Calls 4 other methods internally with ordering constraints
builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryStreams("mississippi-streaming");  // Magic string!
    siloBuilder.AddMemoryGrainStorage("PubSubStore");
    siloBuilder.AddMemoryGrainStorage("event-log");  // Magic string!
    // ... more storage configuration
});

var host = builder.Build();
await host.UseSpringSilo().RunAsync();
```

---

## Target State

### Target Sample Code (Spring.Silo)

```csharp
// Target: 3 lines + Orleans clustering
var builder = Host.CreateApplicationBuilder(args);

builder.AddMississippiSilo()
       .AddSpringDomain()
       .AddInMemoryProviders();  // Or .AddCosmosProviders()

await builder.Build().RunAsync();
```

### What Changed

| Before | After |
|--------|-------|
| `AddSpringSilo()` + manual Orleans | `AddMississippiSilo()` - framework handles Orleans defaults |
| 5+ storage calls in lambda | `.AddInMemoryProviders()` - single composite |
| `UseSpringSilo()` on host | Removed - nothing needed at runtime |
| Ordering-sensitive calls | Internally ordered; safe to call in any order |

---

## Registration Layers

### Layer 1: Framework (Mississippi)

Framework-level setup that all Mississippi apps need. Called once per host.

| Host Type | Method | What It Registers |
|-----------|--------|-------------------|
| Client | `AddMississippiClient()` | SignalR client, HTTP handlers, base services |
| Server | `AddMississippiServer()` | SignalR hubs, API endpoints, Orleans client |
| Silo | `AddMississippiSilo()` | Orleans silo, grain services, stream infrastructure |

**Implementation Strategy:**

```csharp
public static MississippiClientBuilder AddMississippiClient(this WebAssemblyHostBuilder builder)
{
    // Framework infrastructure (idempotent)
    builder.Services.TryAddMississippiClientCore();
    
    // Return builder for fluent chaining
    return new MississippiClientBuilder(builder);
}
```

### Layer 2: Domain (App-Specific)

Domain-level setup for aggregates, projections, commands. **Generated** from `[GenerateMississippiApp]`.

| Method | What It Registers |
|--------|-------------------|
| `Add{App}Domain()` | All aggregates, projections, reducers, effects for the domain |

**Single Domain Registration:**

```csharp
// Generated from [GenerateMississippiApp("Spring")]
public static class SpringDomainRegistrations
{
    public static MississippiClientBuilder AddSpringDomain(this MississippiClientBuilder builder)
    {
        // All aggregate features
        builder.AddProjectAggregate();
        builder.AddDocumentAggregate();
        builder.AddBlogAggregate();
        
        // All projections
        builder.AddProjectProjections();
        builder.AddDocumentProjections();
        
        return builder;
    }
    
    // Overloads for Server and Silo builders
    public static MississippiServerBuilder AddSpringDomain(this MississippiServerBuilder builder) { ... }
    public static MississippiSiloBuilder AddSpringDomain(this MississippiSiloBuilder builder) { ... }
}
```

### Layer 3: Providers (Storage/Infrastructure)

Provider-level setup for persistence, streaming, clustering. **Explicitly separate and pluggable.**

| Provider Set | Method | What It Configures |
|--------------|--------|-------------------|
| In-Memory | `AddInMemoryProviders()` | Memory streams, memory grain storage |
| Cosmos | `AddCosmosProviders()` | Cosmos streams, Cosmos grain storage, Cosmos event store |
| Azure | `AddAzureProviders()` | Blob storage, Azure Queues, Event Hubs |

**Provider Registration:**

```csharp
public static MississippiSiloBuilder AddCosmosProviders(
    this MississippiSiloBuilder builder,
    Action<MississippiCosmosOptions>? configure = null)
{
    var options = new MississippiCosmosOptions();
    configure?.Invoke(options);
    
    builder.UseOrleans(siloBuilder =>
    {
        // Streaming - uses options.StreamProviderName (defaults to "mississippi-streaming")
        siloBuilder.AddCosmosStreams(options.StreamProviderName, options.ConnectionName);
        
        // Grain Storage - uses options.StorageNames (defaults are encapsulated)
        siloBuilder.AddCosmosGrainStorage(options.StorageNames.PubSub, ...);
        siloBuilder.AddCosmosGrainStorage(options.StorageNames.EventLog, ...);
        siloBuilder.AddCosmosGrainStorage(options.StorageNames.Snapshots, ...);
        
        // Event Store
        siloBuilder.AddCosmosEventStore(...);
    });
    
    return builder;
}
```

---

## Fluent Builder API

### Builder Types

```csharp
// Wraps host builder, enables fluent chaining and layer validation
public sealed class MississippiClientBuilder
{
    private WebAssemblyHostBuilder Builder { get; }
    public IServiceCollection Services => Builder.Services;
    
    // Track what's been registered for validation
    internal bool HasDomain { get; set; }
    internal bool HasProviders { get; set; }
}

public sealed class MississippiServerBuilder
{
    private WebApplicationBuilder Builder { get; }
    public IServiceCollection Services => Builder.Services;
    public ConfigurationManager Configuration => Builder.Configuration;
}

public sealed class MississippiSiloBuilder
{
    private HostApplicationBuilder Builder { get; }
    public IServiceCollection Services => Builder.Services;
    
    // Orleans configuration accumulator
    internal List<Action<ISiloBuilder>> OrleansConfigurations { get; } = [];
    
    public MississippiSiloBuilder UseOrleans(Action<ISiloBuilder> configure)
    {
        OrleansConfigurations.Add(configure);
        return this;
    }
}
```

### Build-Time Validation

```csharp
public WebAssemblyHost Build(this MississippiClientBuilder builder)
{
    // Validate required layers
    if (!builder.HasDomain)
        throw new InvalidOperationException("Call Add{App}Domain() before Build()");
    
    // Apply accumulated Orleans config
    foreach (var config in builder.OrleansConfigurations)
        builder.HostBuilder.UseOrleans(config);
    
    return builder.HostBuilder.Build();
}
```

---

## Generated Code Strategy

### Single Source Attribute

```csharp
// In domain assembly
[assembly: GenerateMississippiApp(
    Name = "Spring",
    Aggregates = typeof(ProjectAggregateState),  // Marker for aggregate scanning
    Projections = typeof(ProjectListProjection)  // Marker for projection scanning
)]
```

### Generated Output Per Host Type

**Client (`Spring.Client.Generated.cs`):**

```csharp
public static class SpringClientRegistrations
{
    public static MississippiClientBuilder AddSpringDomain(this MississippiClientBuilder builder)
    {
        // Aggregate client features (commands, SignalR subscriptions)
        builder.AddProjectAggregateFeature();
        builder.AddDocumentAggregateFeature();
        builder.AddBlogAggregateFeature();
        
        // Projection subscriptions
        builder.AddProjectProjectionFeature();
        builder.AddDocumentProjectionFeature();
        
        return builder;
    }
}
```

**Server (`Spring.Server.Generated.cs`):**

```csharp
public static class SpringServerRegistrations
{
    public static MississippiServerBuilder AddSpringDomain(this MississippiServerBuilder builder)
    {
        // SignalR hub mappings
        builder.MapProjectHub();
        builder.MapDocumentHub();
        builder.MapBlogHub();
        
        // API endpoints
        builder.MapProjectEndpoints();
        builder.MapDocumentEndpoints();
        
        return builder;
    }
}
```

**Silo (`Spring.Silo.Generated.cs`):**

```csharp
public static class SpringSiloRegistrations
{
    public static MississippiSiloBuilder AddSpringDomain(this MississippiSiloBuilder builder)
    {
        // Aggregate grains
        builder.AddProjectAggregate();
        builder.AddDocumentAggregate();
        builder.AddBlogAggregate();
        
        // Projection grains
        builder.AddProjectProjections();
        builder.AddDocumentProjections();
        
        // Reducers
        builder.AddProjectReducers();
        
        return builder;
    }
}
```

---

## Options Classes

### MississippiClientOptions

```csharp
public sealed class MississippiClientOptions
{
    /// <summary>Base URI for SignalR and API calls.</summary>
    public Uri? BaseAddress { get; set; }
    
    /// <summary>Enable automatic reconnection for SignalR.</summary>
    public bool AutoReconnect { get; set; } = true;
    
    /// <summary>SignalR hub path prefix.</summary>
    public string HubPathPrefix { get; set; } = "/hubs";
}
```

### MississippiServerOptions

```csharp
public sealed class MississippiServerOptions
{
    /// <summary>Enable CORS for Blazor WASM clients.</summary>
    public bool EnableCors { get; set; } = true;
    
    /// <summary>API endpoint prefix.</summary>
    public string ApiPrefix { get; set; } = "/api";
    
    /// <summary>SignalR hub path prefix.</summary>
    public string HubPathPrefix { get; set; } = "/hubs";
}
```

### MississippiSiloOptions

```csharp
public sealed class MississippiSiloOptions
{
    /// <summary>Stream provider name used for Orleans streaming.</summary>
    public string StreamProviderName { get; set; } = "mississippi-streaming";
    
    /// <summary>Enable Orleans dashboard. Defaults to true in Development.</summary>
    public bool EnableDashboard { get; set; } = true;
}
```

### MississippiCosmosOptions

```csharp
public sealed class MississippiCosmosOptions
{
    /// <summary>Aspire connection name for Cosmos DB.</summary>
    public string ConnectionName { get; set; } = "cosmos-db";
    
    /// <summary>Database ID for Mississippi data.</summary>
    public string DatabaseId { get; set; } = "mississippi";
    
    /// <summary>Stream provider name (inherited from silo options if not set).</summary>
    public string StreamProviderName { get; set; } = "mississippi-streaming";
    
    /// <summary>Container prefix for all Mississippi containers.</summary>
    public string ContainerPrefix { get; set; } = "";
    
    /// <summary>Enable automatic container creation.</summary>
    public bool AutoCreateContainers { get; set; } = true;
    
    /// <summary>Storage provider names for Orleans grain storage.</summary>
    public StorageProviderNames StorageNames { get; set; } = new();
}

/// <summary>Encapsulates Orleans storage provider names.</summary>
public sealed class StorageProviderNames
{
    public string PubSub { get; set; } = "PubSubStore";
    public string EventLog { get; set; } = "event-log";
    public string Snapshots { get; set; } = "snapshots";
    public string Projections { get; set; } = "projections";
}
```

---

## Escape Hatches

### Layer Decomposition

When the composite doesn't fit, decompose to lower-level registrations:

```csharp
// Instead of:
builder.AddMississippiSilo()
       .AddSpringDomain()
       .AddCosmosProviders();

// Decomposed:
builder.AddMississippiSilo(options =>
{
    options.StreamProviderName = "custom-streams";
})
.AddSpringDomain()
.UseOrleans(siloBuilder =>
{
    // Full Orleans control
    siloBuilder.AddCosmosStreams("custom-streams", cosmosOptions =>
    {
        cosmosOptions.ConfigureCosmosClient(clientBuilder => { ... });
    });
    
    // Custom storage providers
    siloBuilder.AddAzureBlobGrainStorage("PubSubStore", blobOptions => { ... });
    siloBuilder.AddCosmosGrainStorage("event-log", ...);  // Use your own name
});
```

### Individual Aggregate Control

```csharp
// Instead of AddSpringDomain(), register aggregates individually:
builder.AddMississippiSilo()
       .AddProjectAggregate()
       .AddDocumentAggregate(options =>
       {
           options.SnapshotFrequency = 50;
           options.EnableCaching = true;
       })
       // Skip BlogAggregate entirely
       .AddCosmosProviders();
```

### Raw Service Access

```csharp
// Escape to raw IServiceCollection
builder.AddMississippiSilo()
       .AddSpringDomain()
       .Services.AddSingleton<ICustomService, CustomService>();
```

---

## Aspire Integration

### Silo with Aspire

```csharp
// AppHost
var cosmos = builder.AddAzureCosmosDB("cosmos")
    .AddDatabase("mississippi");

var silo = builder.AddProject<Projects.Spring_Silo>("spring-silo")
    .WithReference(cosmos)
    .WaitFor(cosmos);
```

```csharp
// Spring.Silo/Program.cs
var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddMississippiSilo()
       .AddSpringDomain()
       .AddCosmosProviders();  // Auto-discovers "cosmos-db" connection

await builder.Build().RunAsync();
```

### Connection Name Override

```csharp
// When Aspire uses different connection name
builder.AddMississippiSilo()
       .AddSpringDomain()
       .AddCosmosProviders(options =>
       {
           options.ConnectionName = "my-cosmos";
       });
```

---

## Implementation Roadmap

### Phase 1: Foundation (Week 1-2)

- [ ] Create `MississippiClientBuilder`, `MississippiServerBuilder`, `MississippiSiloBuilder`
- [ ] Create `MississippiClientOptions`, `MississippiServerOptions`, `MississippiSiloOptions`
- [ ] Implement `AddMississippiClient()`, `AddMississippiServer()`, `AddMississippiSilo()`
- [ ] Make all existing infrastructure registrations idempotent

### Phase 2: Providers (Week 2-3)

- [ ] Create `MississippiCosmosOptions`
- [ ] Implement `AddInMemoryProviders()` for all host types
- [ ] Implement `AddCosmosProviders()` for Silo
- [ ] Wire Aspire connection discovery

### Phase 3: Generators (Week 3-4)

- [ ] Create `[GenerateMississippiApp]` attribute
- [ ] Implement unified domain generator producing:
  - `Add{App}Domain(MississippiClientBuilder)`
  - `Add{App}Domain(MississippiServerBuilder)`
  - `Add{App}Domain(MississippiSiloBuilder)`
- [ ] Remove legacy generators or mark deprecated

### Phase 4: Migration (Week 4-5)

- [ ] Migrate Spring sample to new API
- [ ] Migrate Crescent sample to new API
- [ ] Update documentation
- [ ] Mark legacy methods `[Obsolete]` with migration guidance

---

## Breaking Changes

### Removed APIs

| Removed | Replacement |
|---------|-------------|
| `AddSpringClient()` | `AddMississippiClient().AddSpringDomain()` |
| `AddSpringSilo()` | `AddMississippiSilo().AddSpringDomain()` |
| `UseSpringServer()` | Removed (no runtime phase needed) |
| `UseSpringSilo()` | Removed (no runtime phase needed) |
| Individual `Add{Agg}Aggregate()` in Program.cs | Hidden inside `AddSpringDomain()` |

### Migration Path

```csharp
// BEFORE
builder.AddSpringClient();

// AFTER  
builder.AddMississippiClient()
       .AddSpringDomain();
```

```csharp
// BEFORE
builder.AddSpringSilo();
builder.UseOrleans(siloBuilder => {
    siloBuilder.AddMemoryStreams("mississippi-streaming");  // Magic string
    siloBuilder.AddMemoryGrainStorage("PubSubStore");
    // ... 5 more lines
});
var host = builder.Build();
await host.UseSpringSilo().RunAsync();

// AFTER
builder.AddMississippiSilo()
       .AddSpringDomain()
       .AddInMemoryProviders();

await builder.Build().RunAsync();
```

---

## Example: Minimal Spring App (Target State)

### Spring.Client/Program.cs

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.AddMississippiClient()
       .AddSpringDomain();

await builder.Build().RunAsync();
```

### Spring.Server/Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddMississippiServer()
       .AddSpringDomain();

var app = builder.Build();
app.MapDefaultEndpoints();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

await app.RunAsync();
```

### Spring.Silo/Program.cs

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddMississippiSilo()
       .AddSpringDomain()
       .AddCosmosProviders();

await builder.Build().RunAsync();
```

### What's NOT in Program.cs

- Stream provider name configuration
- Individual aggregate registration
- Storage provider setup (beyond choosing Cosmos vs Memory)
- SignalR hub mapping
- API endpoint mapping
- Ordering constraints
- `UseX()` runtime methods

All handled internally by the three layers.

---

## Appendix: Current vs Target Comparison

### Lines of Code (Spring.Silo)

| Version | Lines | Concepts Visible |
|---------|-------|------------------|
| Current | 15+ | Orleans, streaming, storage names, ordering |
| Target | 4 | Mississippi, domain, providers |

### Registration Method Count (Spring)

| Version | Client Methods | Server Methods | Silo Methods |
|---------|----------------|----------------|--------------|
| Current | 3+ | 4+ | 6+ |
| Target | 1 | 1 | 1 |

### Knowledge Required

| Version | Developer Must Know |
|---------|---------------------|
| Current | Orleans streams, storage providers, SignalR hubs, ordering constraints |
| Target | "Call AddMississippiX, AddDomain, AddProviders" |

---

## Open Questions

1. **Should providers be separate NuGet packages?**
   - Pro: Smaller dependency footprint
   - Con: More packages to manage
   - Recommendation: Yes, `Mississippi.Providers.Cosmos`, `Mississippi.Providers.Memory`

2. **Should clustering be part of providers or separate?**
   - Current design has it separate (explicit Orleans lambda)
   - Could be `AddLocalDevelopment()` vs `AddAzureClustering()`

3. **How to handle multi-domain apps?**
   - Chain: `.AddSpringDomain().AddCrescentDomain()`
   - Separate validation per domain

4. **Telemetry configuration?**
   - Could be part of `AddServiceDefaults()` (Aspire pattern)
   - Or `MississippiSiloOptions.EnableTelemetry`

---

## Summary

The redesigned registration API achieves:

- **3-line host setup** for typical apps
- **Zero-config defaults** via Options classes (no god constants)
- **Single composite generator** replacing multiple partial generators
- **Provider pluggability** as an explicit, separate layer
- **Full escape hatches** when customization is needed
- **Removed `Use*()` runtime methods** - everything happens at registration time
- **Aspire-first design** with connection name discovery

The implementation roadmap is staged to allow incremental adoption while maintaining backward compatibility through `[Obsolete]` markers.
