# Registration Redesign Implementation Approach

> **Status:** Implementation Plan  
> **Prerequisite:** [Registration Design Notes](./registration-notes.md)  
> **Branch:** `feature/inlet-client-composite-generator`

---

## Executive Summary

This document outlines the step-by-step implementation approach to transform Mississippi's registration system from scattered, ordering-sensitive, magic-string-laden setup into a clean 3-line pattern with generated composites and options-driven defaults.

**Target:** 
```csharp
builder.AddMississippiSilo()
       .AddSpringDomain()
       .AddCosmosProviders();
```

---

## ⚠️ Dead Code Removal Note

**IMPORTANT:** As each phase completes, remove the manual code it replaces. Do not leave dead code in the repository.

| Phase | Code to Remove After Completion |
|-------|--------------------------------|
| Phase 0 | Orphaned files outside `Infrastructure/`, duplicate constants, build artifacts |
| Phase 1 | `SpringServerCompositeRegistrations.cs` (manual → generated) |
| Phase 2 | `SpringSiloCompositeRegistrations.cs` (manual → generated) |
| Phase 3 | `MississippiDefaults.cs` (mark `[Obsolete]` first, then remove in v2.0) |
| Phase 5 | Individual `Spring*Registrations.cs` files if fully absorbed into composites |

---

## Source Generator Strategy

**Principle:** Generate everything that can be derived from domain types. Manual code is a bug waiting to happen.

### Why Source Generators?

| Aspect | Manual Registration | Source Generated |
|--------|---------------------|------------------|
| **New aggregate** | Update 3+ files across Client/Server/Silo | Add attribute, rebuild |
| **Rename** | Find/replace across files, hope you got them all | Automatic |
| **Ordering bugs** | Developer must know hidden dependencies | Generator emits correct order |
| **Discoverability** | Read docs or copy from samples | IntelliSense shows `Add{App}Domain()` |
| **Consistency** | Varies by developer | Always follows same pattern |

### What Should Be Generated

| Component | Generate? | Trigger | Output |
|-----------|-----------|---------|--------|
| Client composite | ✅ Yes | `[GenerateInletClientComposite]` | `Add{App}Inlet()` |
| Server composite | ✅ Yes | `[GenerateInletServerComposite]` | `Add{App}Server()`, `Use{App}Server()` |
| Silo composite | ✅ Yes | `[GenerateInletSiloComposite]` | `Add{App}Silo()`, `Use{App}Silo()` |
| Aggregate registrations | ✅ Yes (exists) | `[GenerateCommand]` | `Add{Agg}Aggregate()` |
| Projection registrations | ✅ Yes (exists) | `[GenerateProjectionEndpoints]` | `Add{Proj}Projection()` |
| Provider composites | ❌ No | N/A | Manual `AddCosmosProviders()` - provider choice is explicit |
| Options classes | ❌ No | N/A | Manual - defines the configuration surface |
| Fluent builders | ❌ No | N/A | Manual - framework infrastructure |

### Generator Design Principles

1. **Single attribute triggers full composite** - One `[GenerateMississippiApp("Spring")]` should emit all three host composites
2. **Discover domain types automatically** - Scan for `[GenerateCommand]`, `[GenerateProjectionEndpoints]` in referenced assemblies
3. **Emit explicit code** - No reflection, no assembly scanning at runtime
4. **Include XML docs** - Generated methods should have full IntelliSense documentation
5. **Deterministic output** - Same input always produces same output (sorted, stable)

---

## Current State Inventory

### What's Done (Keep)

| Component | Location | Quality | Notes |
|-----------|----------|---------|-------|
| `InletClientCompositeGenerator` | `src/Inlet.Client.Generators/` | ✅ Good | Generates `Add{App}Inlet()` from attribute |
| `[GenerateInletClientComposite]` | `src/Inlet.Generators.Abstractions/` | ✅ Good | Assembly-level trigger |
| Spring.Client using generated code | `samples/Spring/Spring.Client/Program.cs` | ✅ Good | 3 lines |
| Spring.Server layered registrations | `samples/Spring/Spring.Server/Infrastructure/` | ✅ Good | 5 files, well-organized |
| Spring.Silo layered registrations | `samples/Spring/Spring.Silo/Infrastructure/` | ✅ Good | 6 files, well-organized |
| `[PendingSourceGenerator]` markers | Server/Silo composites | ✅ Good | Clear intent for generator targets |

### What's Pending (Build)

| Component | Purpose | Priority |
|-----------|---------|----------|
| Server composite generator | Generate `AddSpringServer()` / `UseSpringServer()` | P1 |
| Silo composite generator | Generate `AddSpringSilo()` / `UseSpringSilo()` | P1 |
| Fluent builder wrappers | `MississippiClientBuilder`, `MississippiServerBuilder`, `MississippiSiloBuilder` | P2 |
| Options classes | Move defaults from `MississippiDefaults` to per-options | P2 |
| Provider composites | `AddInMemoryProviders()`, `AddCosmosProviders()` | P3 |

### What's Dead Code (Remove)

**Immediate Removal (Phase 0):**

| Component | Reason | Action |
|-----------|--------|--------|
| Duplicate `StreamProviderName` constants | Each appears in multiple files per project | Consolidate to one location |
| Build artifacts (`bin/`, `obj/`) | Stale from refactoring | Clean with `git clean -xfd` |
| Any `*Registrations.cs` outside `Infrastructure/` | Old scattered pattern | Delete if exists |

**Deferred Removal (After Generator Replaces):**

| Component | Reason | Remove After |
|-----------|--------|--------------|
| `SpringServerCompositeRegistrations.cs` | Will be generated | Phase 1 complete |
| `SpringSiloCompositeRegistrations.cs` | Will be generated | Phase 2 complete |
| `MississippiDefaults` class | Replaced by options | Phase 3 complete |
| Direct `MississippiDefaults.X` usages | Options should own defaults | Phase 3 complete |

---

## Implementation Phases

### Phase 0: Clean Up Dead Code (Day 1)

**Goal:** Remove unused files and clean up the codebase before starting new implementation.

#### Step 0.1: Verify and Remove Unused Files

Run the following to identify files that are no longer referenced:

```powershell
# Check for any orphaned registration files from before the refactor
Get-ChildItem -Path samples -Recurse -Filter "*Registration*.cs" | 
    ForEach-Object { Write-Host $_.FullName }
```

**Files to evaluate for removal:**

| File | Keep/Remove | Reason |
|------|-------------|--------|
| `Spring.Server/Infrastructure/SpringServerCompositeRegistrations.cs` | **Keep (for now)** | Reference implementation for generator; remove after Phase 1 |
| `Spring.Silo/Infrastructure/SpringSiloCompositeRegistrations.cs` | **Keep (for now)** | Reference implementation for generator; remove after Phase 2 |
| Any `*Registrations.cs` not in `Infrastructure/` folder | **Remove** | Old scattered pattern |

#### Step 0.2: Clean Build Artifacts

```powershell
# Remove bin/obj to ensure clean state
Get-ChildItem -Path samples -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force
git clean -xfd samples/
```

#### Step 0.3: Verify No Dead Usings

```powershell
# Build and check for unused using warnings
dotnet build samples.slnx -c Release -warnaserror
```

#### Step 0.4: Remove Redundant Constants

Search for and consolidate duplicate constants:

```powershell
# Find all StreamProviderName definitions
Select-String -Path samples/**/*.cs -Pattern 'StreamProviderName\s*=' -SimpleMatch
```

**Action:** Each project should have ONE constant or flow from options, not multiple scattered definitions.

#### Acceptance Criteria for Phase 0

- [ ] No orphaned registration files outside `Infrastructure/` folders
- [ ] Clean build with zero warnings
- [ ] No duplicate `StreamProviderName` constants in same project
- [ ] All build artifacts cleaned

---

### Phase 1: Server Composite Generator (Week 1)

**Goal:** Generate `AddSpringServer()` and `UseSpringServer()` from `[GenerateInletServerComposite]`.

#### Step 1.1: Create Attribute

```csharp
// src/Inlet.Generators.Abstractions/GenerateInletServerCompositeAttribute.cs
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class GenerateInletServerCompositeAttribute : Attribute
{
    public string? AppName { get; set; }
    public string HubPath { get; set; } = "/hubs/inlet";
    public string ApiPrefix { get; set; } = "/api";
}
```

#### Step 1.2: Create Generator

Location: `src/Inlet.Server.Generators/InletServerCompositeGenerator.cs`

The generator should:
1. Find all aggregates (via `[GenerateCommand]`)
2. Find all projections (via `[GenerateProjectionEndpoints]`)
3. Emit `Add{App}Server()` that calls:
   - Observability setup
   - Orleans client setup
   - API registrations
   - Real-time (SignalR, Aqueduct, Inlet)
4. Emit `Use{App}Server()` that calls:
   - Middleware
   - Endpoint mapping

**Reference Implementation:** `samples/Spring/Spring.Server/Infrastructure/SpringServerCompositeRegistrations.cs`

#### Step 1.3: Update Spring.Server

```csharp
// samples/Spring/Spring.Server/Properties/AssemblyInfo.cs
[assembly: GenerateInletServerComposite(AppName = "Spring")]
```

#### Step 1.4: Verify & Remove Manual Code

- Run build, verify generated code matches manual
- Remove `SpringServerCompositeRegistrations.cs`
- Update Program.cs to use generated code

---

### Phase 2: Silo Composite Generator (Week 2)

**Goal:** Generate `AddSpringSilo()` and `UseSpringSilo()` from `[GenerateInletSiloComposite]`.

#### Step 2.1: Create Attribute

```csharp
// src/Inlet.Generators.Abstractions/GenerateInletSiloCompositeAttribute.cs
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class GenerateInletSiloCompositeAttribute : Attribute
{
    public string? AppName { get; set; }
    public string StreamProviderName { get; set; } = "mississippi-streaming";
}
```

#### Step 2.2: Create Generator

Location: `src/Inlet.Silo.Generators/InletSiloCompositeGenerator.cs`

The generator should emit `Add{App}Silo()` that calls:
- Observability
- Aspire resource setup
- Domain (aggregates, projections)
- Event sourcing infrastructure
- Orleans silo configuration

**Reference Implementation:** `samples/Spring/Spring.Silo/Infrastructure/SpringSiloCompositeRegistrations.cs`

#### Step 2.3: Consolidate Layered Registrations

The silo has 6 registration files that need to be templated:
- `SpringAspireRegistrations.cs` → Provider-specific (Cosmos, Blob)
- `SpringDomainRegistrations.cs` → Generated from aggregates/projections
- `SpringEventSourcingRegistrations.cs` → Provider-specific (Cosmos)
- `SpringHealthRegistrations.cs` → Standard health check
- `SpringObservabilityRegistrations.cs` → OpenTelemetry
- `SpringOrleansRegistrations.cs` → Orleans silo config

**Key Insight:** Not all of these should be generated. Domain is generated, providers are composites, observability is boilerplate.

---

### Phase 3: Eliminate MississippiDefaults (Week 3)

**Goal:** Move all defaults into their consuming options classes.

#### Step 3.1: Create Options Classes

Each options class owns its own defaults:

```csharp
// src/Inlet.Server.Abstractions/InletServerOptions.cs (already exists)
public sealed class InletServerOptions
{
    public string StreamProviderName { get; set; } = "mississippi-streaming";
    public string AllClientsStreamNamespace { get; set; } = "mississippi-all-clients";
    public string ServerStreamNamespace { get; set; } = "mississippi-server";
}

// src/EventSourcing.Brooks.Abstractions/BrooksOptions.cs (new)
public sealed class BrooksOptions
{
    public string StreamProviderName { get; set; } = "mississippi-streaming";
}

// src/EventSourcing.Brooks.Cosmos/CosmosBrooksOptions.cs (exists, update)
public sealed class CosmosBrooksOptions
{
    public string DatabaseId { get; set; } = "mississippi";
    public string ContainerId { get; set; } = "brooks";
    public string LockContainerId { get; set; } = "locks";
    public string CosmosClientServiceKey { get; set; } = "mississippi-cosmos-brooks-client";
}
```

#### Step 3.2: Mark MississippiDefaults Obsolete

```csharp
[Obsolete("Use options classes instead. MississippiDefaults will be removed in v2.0.")]
public static class MississippiDefaults { ... }
```

#### Step 3.3: Update All Usages

Pattern replacement:
- `MississippiDefaults.StreamProviderName` → Use injected options
- `MississippiDefaults.DatabaseId` → Use `CosmosBrooksOptions.DatabaseId`
- `MississippiDefaults.ServiceKeys.X` → Use options property

---

### Phase 4: Fluent Builder Wrappers (Week 4)

**Goal:** Create builder types that enable fluent chaining and layer validation.

#### Step 4.1: Create Builder Types

```csharp
// src/Sdk.Client/MississippiClientBuilder.cs
public sealed class MississippiClientBuilder
{
    internal WebAssemblyHostBuilder HostBuilder { get; }
    public IServiceCollection Services => HostBuilder.Services;
    internal bool HasDomain { get; set; }
}

// src/Sdk.Server/MississippiServerBuilder.cs
public sealed class MississippiServerBuilder
{
    internal WebApplicationBuilder HostBuilder { get; }
    public IServiceCollection Services => HostBuilder.Services;
    public ConfigurationManager Configuration => HostBuilder.Configuration;
    internal bool HasDomain { get; set; }
}

// src/Sdk.Silo/MississippiSiloBuilder.cs
public sealed class MississippiSiloBuilder
{
    internal IHostApplicationBuilder HostBuilder { get; }
    public IServiceCollection Services => HostBuilder.Services;
    internal List<Action<ISiloBuilder>> OrleansConfigurations { get; } = [];
}
```

#### Step 4.2: Create Entry Points

```csharp
// src/Sdk.Client/MississippiClientRegistrations.cs
public static MississippiClientBuilder AddMississippiClient(
    this WebAssemblyHostBuilder builder,
    Action<MississippiClientOptions>? configure = null)
{
    var options = new MississippiClientOptions();
    configure?.Invoke(options);
    
    // Framework infrastructure
    builder.Services.TryAddMississippiClientCore(options);
    
    return new MississippiClientBuilder(builder);
}
```

#### Step 4.3: Update Generators to Target Builders

```csharp
// Generated code target
public static MississippiSiloBuilder AddSpringDomain(
    this MississippiSiloBuilder builder)
{
    builder.Services.AddBankAccountAggregate();
    builder.Services.AddTransactionInvestigationQueueAggregate();
    // ...
    builder.HasDomain = true;
    return builder;
}
```

---

### Phase 5: Provider Composites (Week 5)

**Goal:** Create `AddInMemoryProviders()` and `AddCosmosProviders()` for silos.

#### Step 5.1: In-Memory Providers

```csharp
// src/Sdk.Silo/InMemoryProviderRegistrations.cs
public static MississippiSiloBuilder AddInMemoryProviders(
    this MississippiSiloBuilder builder,
    Action<InMemoryProviderOptions>? configure = null)
{
    var options = new InMemoryProviderOptions();
    configure?.Invoke(options);
    
    builder.UseOrleans(siloBuilder =>
    {
        siloBuilder.AddMemoryStreams(options.StreamProviderName);
        siloBuilder.AddMemoryGrainStorage(options.StorageNames.PubSub);
        siloBuilder.AddMemoryGrainStorage(options.StorageNames.EventLog);
        siloBuilder.AddMemoryGrainStorage(options.StorageNames.Snapshots);
    });
    
    return builder;
}
```

#### Step 5.2: Cosmos Providers

```csharp
// src/Sdk.Silo/CosmosProviderRegistrations.cs
public static MississippiSiloBuilder AddCosmosProviders(
    this MississippiSiloBuilder builder,
    Action<MississippiCosmosOptions>? configure = null)
{
    var options = new MississippiCosmosOptions();
    configure?.Invoke(options);
    
    // Aspire resource discovery
    builder.HostBuilder.AddAzureCosmosClient(options.ConnectionName, ...);
    
    builder.UseOrleans(siloBuilder =>
    {
        siloBuilder.AddCosmosStreams(options.StreamProviderName, ...);
        // ... storage providers
    });
    
    // Event sourcing providers
    builder.Services.AddCosmosBrookStorageProvider(...);
    builder.Services.AddCosmosSnapshotStorageProvider(...);
    
    return builder;
}
```

---

## Code Cleanup Checklist

### Files to Remove After Generators Are Complete

- [ ] `samples/Spring/Spring.Server/Infrastructure/SpringServerCompositeRegistrations.cs`
- [ ] `samples/Spring/Spring.Silo/Infrastructure/SpringSiloCompositeRegistrations.cs`

### Files to Update (Remove Manual Registrations)

- [ ] Individual `Spring*Registrations.cs` files → Keep as generated reference or remove
- [ ] `MississippiDefaults.cs` → Mark obsolete, then remove in v2.0

### Constants to Eliminate

| Current | Replacement |
|---------|-------------|
| `private const string StreamProviderName = "StreamProvider"` | Flow from options |
| `private const string DatabaseId = "spring-db"` | Use `CosmosProviderOptions.DatabaseId` |
| `MississippiDefaults.X` | Use typed options class |

---

## Testing Strategy

### Unit Tests

Each generator needs tests comparing generated output to reference implementations:

```csharp
[Fact]
public void Generator_Produces_Expected_Server_Composite()
{
    var expected = File.ReadAllText("Expected/SpringServerCompositeRegistrations.cs");
    var actual = RunGenerator(sourceWithAttribute);
    Assert.Equal(expected, actual);
}
```

### Integration Tests

- Verify generated code compiles
- Verify DI container resolves all services
- Verify Orleans silo starts with generated config
- Verify SignalR hub connects with generated setup

### Migration Tests

- Verify apps using old API still work (backward compat)
- Verify `[Obsolete]` warnings appear for deprecated APIs

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Breaking existing apps | Phase rollout with `[Obsolete]` before removal |
| Generator bugs | Keep manual composites as fallback until v2.0 |
| Complex Orleans config | Provider composites encapsulate complexity |
| Multi-domain apps | Test chaining: `.AddSpringDomain().AddCrescentDomain()` |

---

## Success Metrics

| Metric | Before | After |
|--------|--------|-------|
| Lines in Program.cs (Silo) | 15+ | 4 |
| Registration files per host | 5-6 manual | 0 (generated) |
| Magic string locations | 9+ | 0 (options-driven) |
| Ordering bugs possible | Yes | No (internal ordering) |
| Time to onboard new aggregate | Hours | Minutes (generator handles) |

---

## Appendix: File Tree After Implementation

```
src/
├── Inlet.Client.Generators/
│   └── InletClientCompositeGenerator.cs ✅ (exists)
├── Inlet.Server.Generators/ (new)
│   └── InletServerCompositeGenerator.cs
├── Inlet.Silo.Generators/ (new)
│   └── InletSiloCompositeGenerator.cs
├── Inlet.Generators.Abstractions/
│   ├── GenerateInletClientCompositeAttribute.cs ✅ (exists)
│   ├── GenerateInletServerCompositeAttribute.cs (new)
│   └── GenerateInletSiloCompositeAttribute.cs (new)
├── Sdk.Client/
│   ├── MississippiClientBuilder.cs (new)
│   └── MississippiClientRegistrations.cs (new)
├── Sdk.Server/
│   ├── MississippiServerBuilder.cs (new)
│   └── MississippiServerRegistrations.cs (new)
├── Sdk.Silo/
│   ├── MississippiSiloBuilder.cs (new)
│   ├── MississippiSiloRegistrations.cs (new)
│   ├── InMemoryProviderRegistrations.cs (new)
│   └── CosmosProviderRegistrations.cs (new)

samples/Spring/
├── Spring.Client/
│   └── Program.cs (3 lines ✅)
├── Spring.Server/
│   ├── Program.cs (4 lines ✅)
│   └── Properties/AssemblyInfo.cs (generator attribute)
├── Spring.Silo/
│   ├── Program.cs (4 lines ✅)
│   └── Properties/AssemblyInfo.cs (generator attribute)
```

---

## Next Steps

1. **Immediate:** Review this plan and confirm priorities
2. **Week 1:** Implement `InletServerCompositeGenerator`
3. **Week 2:** Implement `InletSiloCompositeGenerator`
4. **Week 3:** Migrate `MississippiDefaults` to options
5. **Week 4:** Create fluent builder wrappers
6. **Week 5:** Create provider composites
7. **Week 6:** Clean up dead code, update docs
